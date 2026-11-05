// @Role: Konwersja fotografii kartki do JPEG po stronie klienta (canvas, dłuższy bok ≤ 2000 px, jakość 0.85, orientacja EXIF) oraz walidacja rozmiaru po konwersji (U-25) | @DependsOn: API przeglądarki (createImageBitmap, canvas) | @UsedBy: features/solutions/PhotoUpload | @Task: TASK-136

export const MAKS_BOK_PX = 2000;
export const JAKOSC_JPEG = 0.85;
export const MAKS_BAJTOW_ZDJECIA = 10 * 1024 * 1024;

export type KodBleduZdjecia = "NIECZYTELNE" | "ZA_DUZE";

export class BladZdjecia extends Error {
  readonly kod: KodBleduZdjecia;
  constructor(kod: KodBleduZdjecia) {
    super(kod);
    this.name = "BladZdjecia";
    this.kod = kod;
  }
}

/** Funkcja wstrzykiwana do komponentu (w testach zastępowana atrapą). */
export type KonwerterJpeg = (plik: Blob) => Promise<Blob>;

/**
 * Przeskalowuje obraz do ≤ 2000 px dłuższego boku i koduje jako JPEG (0.85).
 * Przeglądarka respektuje orientację EXIF (`imageOrientation: "from-image"`).
 * Plik, którego przeglądarka nie odczyta (np. HEIC w Chrome), kończy się `BladZdjecia("NIECZYTELNE")`.
 */
export const naJpeg: KonwerterJpeg = async (plik) => {
  let bitmapa: ImageBitmap;
  try {
    bitmapa = await createImageBitmap(plik, { imageOrientation: "from-image" });
  } catch {
    throw new BladZdjecia("NIECZYTELNE");
  }
  try {
    const skala = Math.min(1, MAKS_BOK_PX / Math.max(bitmapa.width, bitmapa.height));
    const szerokosc = Math.max(1, Math.round(bitmapa.width * skala));
    const wysokosc = Math.max(1, Math.round(bitmapa.height * skala));
    const plotno = document.createElement("canvas");
    plotno.width = szerokosc;
    plotno.height = wysokosc;
    const kontekst = plotno.getContext("2d");
    if (!kontekst) throw new BladZdjecia("NIECZYTELNE");
    // JPEG nie ma przezroczystości — przezroczyste piksele (PNG) mają być białe, nie czarne.
    kontekst.fillStyle = "#ffffff";
    kontekst.fillRect(0, 0, szerokosc, wysokosc);
    kontekst.drawImage(bitmapa, 0, 0, szerokosc, wysokosc);
    const wynik = await new Promise<Blob | null>((resolve) => {
      plotno.toBlob(resolve, "image/jpeg", JAKOSC_JPEG);
    });
    if (!wynik) throw new BladZdjecia("NIECZYTELNE");
    return wynik;
  } finally {
    bitmapa.close();
  }
};

/** Konwertuje i sprawdza limit 10 MB po konwersji; błędy jako `BladZdjecia`. */
export async function przygotujZdjecie(plik: Blob, konwerter: KonwerterJpeg): Promise<Blob> {
  let wynik: Blob;
  try {
    wynik = await konwerter(plik);
  } catch (e) {
    throw e instanceof BladZdjecia ? e : new BladZdjecia("NIECZYTELNE");
  }
  if (wynik.size > MAKS_BAJTOW_ZDJECIA) throw new BladZdjecia("ZA_DUZE");
  return wynik;
}
