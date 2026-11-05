// @Role: Testy formularzy logowania i rejestracji, strażnika układu Ucznia, wylogowania i odświeżenia sesji na mockach MSW | @DependsOn: features/accounts, lib/auth, mocks/server, mocks/db | @UsedBy: vitest | @Task: TASK-133
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { HttpResponse, http } from "msw";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { resetujStan } from "@/mocks/db";
import { server } from "@/mocks/server";
import {
  przekieruj,
  przekierujFetchNaLocalhost,
  renderujZSesja,
  ustawSciezke,
} from "@/lib/auth/test-support";
import { LoginForm, RegisterForm, StudentShell } from "./index";

vi.mock("next/navigation", async () => (await import("@/lib/auth/test-support")).nawigacjaMock);

const HASLO_DEMO = "Lemat-Demo-2026!";
let zadania: { metoda: string; sciezka: string }[] = [];

beforeAll(() => {
  server.listen({ onUnhandledRequest: "error" });
  przekierujFetchNaLocalhost();
  server.events.on("request:start", ({ request }) => {
    zadania.push({ metoda: request.method, sciezka: new URL(request.url).pathname });
  });
});
beforeEach(() => {
  resetujStan();
  zadania = [];
  przekieruj.replace.mockClear();
  ustawSciezke("/exercises");
});
afterEach(() => server.resetHandlers());
afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

const liczbaPost = (sciezka: string) =>
  zadania.filter((z) => z.metoda === "POST" && z.sciezka === sciezka).length;

describe("LoginForm", () => {
  it("waliduje pola po polsku i nie wysyła żądania", async () => {
    const u = userEvent.setup();
    renderujZSesja(<LoginForm />);
    await screen.findByRole("button", { name: "Zaloguj się" });
    await u.click(screen.getByRole("button", { name: "Zaloguj się" }));
    expect(await screen.findByText("Podaj adres e-mail.")).toBeInTheDocument();
    expect(screen.getByText("Podaj hasło.")).toBeInTheDocument();
    expect(liczbaPost("/api/v1/auth/logowanie")).toBe(0);
  });

  it("zły login pokazuje czytelny komunikat", async () => {
    const u = userEvent.setup();
    renderujZSesja(<LoginForm />);
    await u.type(await screen.findByLabelText("Adres e-mail"), "uczen1@lemat.local");
    await u.type(screen.getByLabelText("Hasło"), "zle-haslo-123");
    await u.click(screen.getByRole("button", { name: "Zaloguj się" }));
    expect(await screen.findByText("Nieprawidłowy e-mail lub hasło.")).toBeInTheDocument();
    expect(przekieruj.replace).not.toHaveBeenCalled();
  });

  it("udane logowanie przekierowuje na /exercises", async () => {
    const u = userEvent.setup();
    renderujZSesja(<LoginForm />);
    await u.type(await screen.findByLabelText("Adres e-mail"), "uczen1@lemat.local");
    await u.type(screen.getByLabelText("Hasło"), HASLO_DEMO);
    await u.click(screen.getByRole("button", { name: "Zaloguj się" }));
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/exercises"));
  });

  it("podwójne kliknięcie wysyła jedno żądanie logowania", async () => {
    const u = userEvent.setup();
    renderujZSesja(<LoginForm />);
    await u.type(await screen.findByLabelText("Adres e-mail"), "uczen1@lemat.local");
    await u.type(screen.getByLabelText("Hasło"), HASLO_DEMO);
    await u.dblClick(screen.getByRole("button", { name: "Zaloguj się" }));
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/exercises"));
    expect(liczbaPost("/api/v1/auth/logowanie")).toBe(1);
  });

  it("błędy bledy_pol z serwera trafiają na pola", async () => {
    server.use(
      http.post("*/api/v1/auth/logowanie", () =>
        HttpResponse.json(
          {
            type: "https://lemat.pl/bledy/walidacja",
            title: "Błędne dane",
            status: 422,
            detail: "Popraw zaznaczone pola.",
            instance: "/api/v1/auth/logowanie",
            kod: "WALIDACJA",
            trace_id: "x",
            szczegoly: null,
            bledy_pol: [
              { pole: "haslo", kod_reguly: "WYMAGANE", komunikat: "Hasło jest wymagane (serwer)." },
            ],
          },
          { status: 422, headers: { "Content-Type": "application/problem+json" } },
        ),
      ),
    );
    const u = userEvent.setup();
    renderujZSesja(<LoginForm />);
    await u.type(await screen.findByLabelText("Adres e-mail"), "uczen1@lemat.local");
    await u.type(screen.getByLabelText("Hasło"), "cokolwiek");
    await u.click(screen.getByRole("button", { name: "Zaloguj się" }));
    expect(await screen.findByText("Hasło jest wymagane (serwer).")).toBeInTheDocument();
  });
});

describe("RegisterForm", () => {
  async function wypelnij(u: ReturnType<typeof userEvent.setup>, email = "ola@example.com") {
    await u.type(await screen.findByLabelText("Imię"), "Ola");
    await u.type(screen.getByLabelText("Nazwisko"), "Wiśniewska");
    await u.type(screen.getByLabelText("Adres e-mail"), email);
    await u.type(screen.getByLabelText(/Hasło/), "BardzoTajne#2026");
    await u.click(screen.getByLabelText(/ukończyłem/));
    await u.click(screen.getByLabelText(/regulamin/));
  }

  it("waliduje reguły serwera po polsku", async () => {
    const u = userEvent.setup();
    renderujZSesja(<RegisterForm />);
    await u.type(await screen.findByLabelText(/Hasło/), "krotkie");
    await u.type(screen.getByLabelText("Adres e-mail"), "nie-email");
    await u.click(screen.getByRole("button", { name: "Załóż konto" }));
    expect(await screen.findByText("Hasło musi mieć co najmniej 10 znaków.")).toBeInTheDocument();
    expect(screen.getByText("Podaj poprawny adres e-mail.")).toBeInTheDocument();
    expect(screen.getByText("Podaj imię.")).toBeInTheDocument();
    expect(screen.getByText("Podaj nazwisko.")).toBeInTheDocument();
    expect(screen.getByText("Wymagane oświadczenie o ukończeniu 16 lat.")).toBeInTheDocument();
    expect(screen.getByText("Wymagana akceptacja regulaminu.")).toBeInTheDocument();
    expect(liczbaPost("/api/v1/auth/rejestracja")).toBe(0);
  });

  it("rejestracja tworzy sesję i przekierowuje", async () => {
    const u = userEvent.setup();
    renderujZSesja(<RegisterForm />);
    await wypelnij(u);
    await u.click(screen.getByRole("button", { name: "Załóż konto" }));
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/exercises"));
  });

  it("EMAIL_ZAJETY pokazuje komunikat bez ujawniania konta", async () => {
    const u = userEvent.setup();
    renderujZSesja(<RegisterForm />);
    await wypelnij(u, "uczen1@lemat.local");
    await u.click(screen.getByRole("button", { name: "Załóż konto" }));
    expect(
      await screen.findByText(
        "Nie udało się założyć konta na ten adres — zaloguj się albo zresetuj hasło.",
      ),
    ).toBeInTheDocument();
    expect(przekieruj.replace).not.toHaveBeenCalled();
  });

  it("bledy_pol z serwera trafiają na pola", async () => {
    server.use(
      http.post("*/api/v1/auth/rejestracja", () =>
        HttpResponse.json(
          {
            type: "https://lemat.pl/bledy/walidacja",
            title: "Błędne dane",
            status: 422,
            detail: "Popraw zaznaczone pola.",
            instance: "/api/v1/auth/rejestracja",
            kod: "WALIDACJA",
            trace_id: "x",
            szczegoly: null,
            bledy_pol: [
              {
                pole: "imie",
                kod_reguly: "ZA_DLUGIE",
                komunikat: "Imię jest zbyt długie (serwer).",
              },
            ],
          },
          { status: 422, headers: { "Content-Type": "application/problem+json" } },
        ),
      ),
    );
    const u = userEvent.setup();
    renderujZSesja(<RegisterForm />);
    await wypelnij(u);
    await u.click(screen.getByRole("button", { name: "Załóż konto" }));
    expect(await screen.findByText("Imię jest zbyt długie (serwer).")).toBeInTheDocument();
  });

  it("podwójne kliknięcie wysyła jedno żądanie rejestracji", async () => {
    const u = userEvent.setup();
    renderujZSesja(<RegisterForm />);
    await wypelnij(u);
    await u.dblClick(screen.getByRole("button", { name: "Załóż konto" }));
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/exercises"));
    expect(liczbaPost("/api/v1/auth/rejestracja")).toBe(1);
  });
});

describe("StudentShell (strażnik)", () => {
  it("niezalogowany jest przekierowany na /login i nie widzi treści", async () => {
    renderujZSesja(
      <StudentShell>
        <p>Treść chroniona</p>
      </StudentShell>,
    );
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/login"));
    expect(screen.queryByText("Treść chroniona")).not.toBeInTheDocument();
  });

  async function zalogowanyShell() {
    const u = userEvent.setup();
    renderujZSesja(
      <>
        <StudentShell>
          <p>Treść chroniona</p>
        </StudentShell>
      </>,
    );
    return u;
  }

  it("po wcześniejszym logowaniu odświeżenie przy starcie przywraca sesję, a nawigacja ma wszystkie pozycje", async () => {
    // Stan „cookie” mocka: logowanie wykonane wcześniej (np. w poprzedniej wizycie).
    const { zaloguj, skonfigurujKlienta } = await import("@/lib/api/client");
    skonfigurujKlienta({ pobierzToken: () => null, odswiezSesje: () => Promise.resolve(null) });
    await zaloguj({ email: "uczen1@lemat.local", haslo: HASLO_DEMO });
    await zalogowanyShell();
    expect(await screen.findByText("Treść chroniona")).toBeInTheDocument();
    const nav = screen.getByRole("navigation", { name: "Menu ucznia" });
    for (const nazwa of ["Zadania", "Rozwiązania", "Profil", "Ustawienia", "Wyloguj"]) {
      expect(nav).toHaveTextContent(nazwa);
    }
    expect(nav.className).toContain("flex-wrap"); // zawijanie na wąskim ekranie (375 px)
    expect(przekieruj.replace).not.toHaveBeenCalled();
    expect(liczbaPost("/api/v1/auth/odswiezenie")).toBe(1);
  });

  it("wylogowanie wywołuje endpoint, czyści stan i przekierowuje na /login", async () => {
    const { zaloguj, skonfigurujKlienta } = await import("@/lib/api/client");
    skonfigurujKlienta({ pobierzToken: () => null, odswiezSesje: () => Promise.resolve(null) });
    await zaloguj({ email: "uczen1@lemat.local", haslo: HASLO_DEMO });
    const u = await zalogowanyShell();
    await screen.findByText("Treść chroniona");
    await u.click(screen.getByRole("button", { name: "Wyloguj" }));
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/login"));
    expect(liczbaPost("/api/v1/auth/wylogowanie")).toBe(1);
    expect(screen.queryByText("Treść chroniona")).not.toBeInTheDocument();
  });

  it("nieudane odświeżenie SESJA_UNIEWAZNIONA przy starcie kończy na /login", async () => {
    server.use(
      http.post("*/api/v1/auth/odswiezenie", () =>
        HttpResponse.json(
          {
            type: "https://lemat.pl/bledy/sesja-uniewazniona",
            title: "Sesja wygasła",
            status: 401,
            detail: "Sesja wygasła.",
            instance: "/api/v1/auth/odswiezenie",
            kod: "SESJA_UNIEWAZNIONA",
            trace_id: "x",
            szczegoly: null,
          },
          { status: 401, headers: { "Content-Type": "application/problem+json" } },
        ),
      ),
    );
    await zalogowanyShell();
    await waitFor(() => expect(przekieruj.replace).toHaveBeenCalledWith("/login"));
  });
});
