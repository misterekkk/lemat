// @Role: Przesłanie zdjęć kartki: wybór 1…5 plików (aparat/galeria), konwersja do JPEG, miniatury z usuwaniem, wysyłka multipart POST /rozwiazania i przejście do ekranu Rozwiązania | @DependsOn: lib/api/client, lib/api/queries, lib/photos/to-jpeg, lib/api/use-error-message, ui/Button | @UsedBy: features/solutions/index, app/(student)/exercises/[exerciseId]/page | @Task: TASK-136
"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState, type ChangeEvent } from "react";
import { przeslijRozwiazanie } from "@/lib/api/client";
import { BladApi } from "@/lib/api/errors";
import { kluczeZapytan, useZadanie } from "@/lib/api/queries";
import type { ScenariuszMock } from "@/lib/api/types";
import { useKomunikatBledu } from "@/lib/api/use-error-message";
import { BladZdjecia, naJpeg, przygotujZdjecie, type KonwerterJpeg } from "@/lib/photos/to-jpeg";
import { Button } from "@/ui/Button";

export const MAKS_ZDJEC = 5;

export interface PhotoUploadProps {
  zadanieId: string;
  /** Wstrzykiwany konwerter (testy); domyślnie canvas → JPEG. */
  konwerter?: KonwerterJpeg;
}

interface Zdjecie {
  id: number;
  nazwa: string;
  blob: Blob;
  podglad: string | null;
}

type BladPrzeslania =
  { rodzaj: "tekst"; tekst: string } | { rodzaj: "istniejace"; rozwiazanieId: string | null };

/** Nagłówek scenariusza mocka wysyłany tylko, gdy ustawiono zmienną (tryb dev/E2E). */
function scenariuszZeSrodowiska(): ScenariuszMock | undefined {
  const wartosc = process.env.NEXT_PUBLIC_LEMAT_MOCK_SCENARIUSZ;
  return wartosc ? (wartosc as ScenariuszMock) : undefined;
}

function utworzPodglad(blob: Blob): string | null {
  return typeof URL.createObjectURL === "function" ? URL.createObjectURL(blob) : null;
}

function zwolnijPodglad(zdjecie: Zdjecie): void {
  if (zdjecie.podglad && typeof URL.revokeObjectURL === "function") {
    URL.revokeObjectURL(zdjecie.podglad);
  }
}

export function PhotoUpload({ zadanieId, konwerter = naJpeg }: PhotoUploadProps) {
  const t = useTranslations("Przeslanie");
  const komunikatBledu = useKomunikatBledu();
  const router = useRouter();
  const klientZapytan = useQueryClient();
  const zadanie = useZadanie(zadanieId).data;
  const wysylkaWToku = useRef(false);

  const [zdjecia, ustawZdjecia] = useState<Zdjecie[]>([]);
  const [przetwarzane, ustawPrzetwarzane] = useState(0);
  const [ostrzezenia, ustawOstrzezenia] = useState<string[]>([]);
  const [wysylanie, ustawWysylanie] = useState(false);
  const [blad, ustawBlad] = useState<BladPrzeslania | null>(null);
  const zdjeciaRef = useRef<Zdjecie[]>([]);
  const licznikId = useRef(0);

  useEffect(() => {
    zdjeciaRef.current = zdjecia;
  }, [zdjecia]);
  useEffect(
    () => () => {
      zdjeciaRef.current.forEach(zwolnijPodglad);
    },
    [],
  );

  async function dodajPliki(zdarzenie: ChangeEvent<HTMLInputElement>) {
    const wybrane = Array.from(zdarzenie.target.files ?? []);
    zdarzenie.target.value = "";
    if (wybrane.length === 0) return;
    ustawBlad(null);

    const nowe: string[] = [];
    const wolne = Math.max(0, MAKS_ZDJEC - zdjeciaRef.current.length);
    const przyjete = wybrane.slice(0, wolne);
    if (wybrane.length > wolne) nowe.push(t("limitZdjec", { maks: MAKS_ZDJEC }));

    ustawPrzetwarzane((n) => n + przyjete.length);
    const wyniki = await Promise.all(
      przyjete.map(async (plik) => {
        try {
          return { plik, blob: await przygotujZdjecie(plik, konwerter) };
        } catch (e) {
          const kod = e instanceof BladZdjecia ? e.kod : "NIECZYTELNE";
          nowe.push(
            kod === "ZA_DUZE"
              ? t("zaDuze", { nazwa: plik.name })
              : t("nieczytelne", { nazwa: plik.name }),
          );
          return null;
        }
      }),
    );
    const dodane: Zdjecie[] = [];
    for (const wynik of wyniki) {
      if (!wynik) continue;
      licznikId.current += 1;
      dodane.push({
        id: licznikId.current,
        nazwa: wynik.plik.name,
        blob: wynik.blob,
        podglad: utworzPodglad(wynik.blob),
      });
    }
    ustawZdjecia((biezace) => {
      const miejsce = Math.max(0, MAKS_ZDJEC - biezace.length);
      dodane.slice(miejsce).forEach(zwolnijPodglad);
      return [...biezace, ...dodane.slice(0, miejsce)];
    });
    ustawOstrzezenia(nowe);
    ustawPrzetwarzane((n) => n - przyjete.length);
  }

  function usun(id: number) {
    ustawZdjecia((biezace) => {
      biezace.filter((z) => z.id === id).forEach(zwolnijPodglad);
      return biezace.filter((z) => z.id !== id);
    });
    ustawOstrzezenia([]);
    ustawBlad(null);
  }

  function opiszBlad(e: unknown): BladPrzeslania {
    if (e instanceof BladApi) {
      if (e.kod === "ROZWIAZANIE_NIEPRAWIDLOWY_STAN") {
        const id = e.szczegoly?.istniejace_rozwiazanie_id;
        return { rodzaj: "istniejace", rozwiazanieId: typeof id === "string" ? id : null };
      }
      if (e.kod === "WERSJA_ZADANIA_NIEAKTUALNA") {
        void klientZapytan.invalidateQueries({ queryKey: kluczeZapytan.zadanie(zadanieId) });
        return { rodzaj: "tekst", tekst: t("wersjaNieaktualna") };
      }
      // K-B-06: 415 (inny format) i 422 ZDJECIE_NIEPRAWIDLOWE to ten sam błąd zdjęcia dla Ucznia.
      if (e.kod === "NIEOBSLUGIWANY_FORMAT" || e.kod === "ZDJECIE_NIEPRAWIDLOWE") {
        return { rodzaj: "tekst", tekst: t("bladZdjecia") };
      }
    }
    return { rodzaj: "tekst", tekst: komunikatBledu(e) };
  }

  async function wyslij() {
    if (!zadanie || zdjecia.length === 0 || przetwarzane > 0) return;
    ustawBlad(null);
    // U-11: blokada działa od razu (ref), zanim React odświeży stan przycisku.
    if (wysylkaWToku.current) return;
    wysylkaWToku.current = true;
    ustawWysylanie(true);
    try {
      const rozwiazanie = await przeslijRozwiazanie({
        zadanie_wersja_id: zadanie.zadanie_wersja_id,
        zdjecia: zdjecia.map((z) => z.blob),
        scenariusz: scenariuszZeSrodowiska(),
      });
      router.push(`/solutions/${rozwiazanie.id}`);
    } catch (e) {
      wysylkaWToku.current = false;
      ustawWysylanie(false);
      ustawBlad(opiszBlad(e));
    }
  }

  const zablokowany = !zadanie || zdjecia.length === 0 || przetwarzane > 0 || wysylanie;

  return (
    <div className="flex flex-col gap-4 rounded-lg border border-slate-300 p-4">
      <h2 className="text-lg font-semibold">{t("naglowek")}</h2>
      <p className="text-sm">{t("podpowiedz", { maks: MAKS_ZDJEC })}</p>

      <div>
        <label className="inline-flex min-h-11 cursor-pointer items-center justify-center rounded-md border border-slate-300 bg-white px-4 py-2 font-medium text-slate-900 hover:bg-slate-50 has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-blue-600">
          {t("dodaj")}
          <input
            type="file"
            accept="image/*"
            capture="environment"
            multiple
            className="sr-only"
            disabled={wysylanie || zdjecia.length >= MAKS_ZDJEC}
            onChange={(e) => void dodajPliki(e)}
          />
        </label>
      </div>

      <p aria-live="polite">{t("licznik", { liczba: zdjecia.length, maks: MAKS_ZDJEC })}</p>
      {przetwarzane > 0 ? <p role="status">{t("przetwarzanie")}</p> : null}
      {ostrzezenia.map((tekst) => (
        <p key={tekst} role="alert" className="rounded-md bg-amber-50 p-3 text-amber-900">
          {tekst}
        </p>
      ))}

      {zdjecia.length > 0 ? (
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {zdjecia.map((zdjecie, indeks) => (
            <li key={zdjecie.id} className="flex flex-col gap-2">
              {zdjecie.podglad ? (
                // Podgląd lokalny (blob:) — next/image nie ma tu zastosowania.
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={zdjecie.podglad}
                  alt={t("miniatura", { nr: indeks + 1 })}
                  className="aspect-[3/4] w-full rounded-md border border-slate-200 object-cover"
                />
              ) : (
                <span className="break-words">{zdjecie.nazwa}</span>
              )}
              <Button
                wariant="drugorzedny"
                disabled={wysylanie}
                aria-label={t("usun", { nr: indeks + 1 })}
                onClick={() => usun(zdjecie.id)}
              >
                {t("usunKrotko")}
              </Button>
            </li>
          ))}
        </ul>
      ) : null}

      {blad ? (
        <div role="alert" className="rounded-md bg-red-50 p-3 text-red-800">
          {blad.rodzaj === "tekst" ? (
            <p>{blad.tekst}</p>
          ) : (
            <>
              <p>{t("istniejace")}</p>
              {blad.rozwiazanieId ? (
                <Link
                  href={`/solutions/${blad.rozwiazanieId}`}
                  className="inline-flex min-h-11 items-center underline"
                >
                  {t("przejdzDoRozwiazania")}
                </Link>
              ) : null}
            </>
          )}
        </div>
      ) : null}

      <div>
        <Button disabled={zablokowany} onClick={() => void wyslij()}>
          {wysylanie ? t("wTrakcie") : t("wyslij")}
        </Button>
      </div>
    </div>
  );
}
