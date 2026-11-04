// @Role: Test kompletności komunikatów bledy.<KOD> w pl-PL.json dla wszystkich kodów z kontraktu 1.2 i kodów klienta | @DependsOn: lib/api/errors, messages/pl-PL.json | @UsedBy: vitest | @Task: TASK-131
import { describe, expect, it } from "vitest";
import komunikaty from "../../../messages/pl-PL.json";
import { BladApi, KODY_KLIENTA, KODY_SERWERA, kluczKomunikatuBledu } from "./errors";

describe("komunikaty błędów", () => {
  const bledy = komunikaty.bledy as Record<string, string>;

  it.each([...KODY_SERWERA, ...KODY_KLIENTA])("istnieje komunikat dla %s", (kod) => {
    expect(bledy[kod]).toBeTruthy();
    expect(kluczKomunikatuBledu(kod)).toBe(`bledy.${kod}`);
  });

  it("nie ma komunikatów dla kodów spoza kontraktu (poza NIEZNANY)", () => {
    const znane = new Set<string>([...KODY_SERWERA, ...KODY_KLIENTA, "NIEZNANY"]);
    for (const kod of Object.keys(bledy)) expect(znane.has(kod)).toBe(true);
  });

  it("zwraca null dla nieznanego kodu", () => {
    expect(kluczKomunikatuBledu("COS_INNEGO")).toBeNull();
  });

  it("BladApi udostępnia szczegóły i aktualny zasób", () => {
    const blad = new BladApi({
      status: 412,
      kod: "KONFLIKT_WERSJI",
      problem: {
        type: "t",
        title: "t",
        status: 412,
        detail: "d",
        instance: "/",
        kod: "KONFLIKT_WERSJI",
        trace_id: "x",
        szczegoly: { aktualny: { version: 3 } },
      },
    });
    expect(blad.aktualny).toEqual({ version: 3 });
    expect(blad.bledyPol).toEqual([]);
    expect(blad.message).toBe("d");
  });
});
