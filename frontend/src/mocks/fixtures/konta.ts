// @Role: Dane mocka: konta demo (POC_CONTRACT 6.1) i miniatura JPEG dla endpointu magazynu lokalnego | @DependsOn: (brak) | @UsedBy: mocks/db, mocks/handlers | @Task: TASK-132
export interface KontoDemo {
  id: string;
  email: string;
  haslo: string;
  imie: string;
  nazwisko: string;
}

export const KONTA_DEMO: KontoDemo[] = [
  {
    id: "c1000000-0000-4000-8000-000000000001",
    email: "uczen1@lemat.local",
    haslo: "Lemat-Demo-2026!",
    imie: "Ala",
    nazwisko: "Kowalska",
  },
  {
    id: "c1000000-0000-4000-8000-000000000002",
    email: "uczen2@lemat.local",
    haslo: "Lemat-Demo-2026!",
    imie: "Jan",
    nazwisko: "Nowak",
  },
];

/** Pierwsze konto zakładane rejestracją ma id …0003 (przykłady kontraktu 3.1). */
export const PIERWSZY_NUMER_KONTA_REJESTROWANEGO = 3;

/** Mały poprawny JPEG 8x8 zwracany przez GET /magazyn-lokalny/{token} (POC_CONTRACT 7.10). */
export const MINIATURA_JPEG_BASE64 =
  "/9j/4AAQSkZJRgABAQAASABIAAD/4QBMRXhpZgAATU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAA6ABAAMAAAABAAEAAKACAAQAAAABAAAACKADAAQAAAABAAAACAAAAAD/wAARCAAIAAgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9sAQwAPDw8PDw8aDw8aJBoaGiQxJCQkJDE+MTExMTE+Sz4+Pj4+PktLS0tLS0tLWlpaWlpaaWlpaWl2dnZ2dnZ2dnZ2/9sAQwESExMeHB40HBw0e1RFVHt7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7e3t7/90ABAAB/9oADAMBAAIRAxEAPwDv+c0tFFMD/9k=";
