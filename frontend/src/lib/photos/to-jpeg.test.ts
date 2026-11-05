// @Role: Testy walidacji po konwersji zdjęcia (limit 10 MB, mapowanie błędów konwertera) | @DependsOn: lib/photos/to-jpeg | @UsedBy: vitest | @Task: TASK-136
import { describe, expect, it } from "vitest";
import { BladZdjecia, MAKS_BAJTOW_ZDJECIA, przygotujZdjecie } from "./to-jpeg";

describe("przygotujZdjecie", () => {
  it("zwraca wynik konwertera w limicie", async () => {
    const wynik = new Blob(["abc"]);
    await expect(przygotujZdjecie(new Blob(["x"]), () => Promise.resolve(wynik))).resolves.toBe(
      wynik,
    );
  });
  it("odrzuca wynik większy niż 10 MB", async () => {
    const duzy = new Blob([new Uint8Array(MAKS_BAJTOW_ZDJECIA + 1)]);
    await expect(
      przygotujZdjecie(new Blob(["x"]), () => Promise.resolve(duzy)),
    ).rejects.toMatchObject({
      kod: "ZA_DUZE",
    });
  });
  it("niespodziewany błąd konwertera to NIECZYTELNE", async () => {
    const wynik = przygotujZdjecie(new Blob(["x"]), () => Promise.reject(new Error("boom")));
    await expect(wynik).rejects.toBeInstanceOf(BladZdjecia);
    await expect(wynik).rejects.toMatchObject({ kod: "NIECZYTELNE" });
  });
});
