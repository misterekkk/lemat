// @Role: Testy podstawowych komponentów src/ui (Button, List, Form, Dialog) | @DependsOn: ui/*, Testing Library | @UsedBy: vitest | @Task: TASK-006
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Button } from "./Button";
import { Dialog } from "./Dialog";
import { FormField } from "./Form";
import { List } from "./List";

describe("src/ui", () => {
  it("Button wywołuje onClick", async () => {
    const onClick = vi.fn();
    render(<Button onClick={onClick}>Kliknij</Button>);
    await userEvent.click(screen.getByRole("button", { name: "Kliknij" }));
    expect(onClick).toHaveBeenCalledOnce();
  });

  it("List pokazuje komunikat dla pustej listy i elementy", () => {
    const { rerender } = render(
      <List elementy={[]} kluczElementu={String} renderElement={String} komunikatPusta="Pusto" />,
    );
    expect(screen.getByText("Pusto")).toBeInTheDocument();
    rerender(
      <List
        elementy={["a", "b"]}
        kluczElementu={String}
        renderElement={String}
        komunikatPusta="Pusto"
      />,
    );
    expect(screen.getAllByRole("listitem")).toHaveLength(2);
  });

  it("FormField łączy etykietę z polem i pokazuje błąd", () => {
    render(<FormField etykieta="Email" blad="Niepoprawny" />);
    const pole = screen.getByLabelText("Email");
    expect(pole).toHaveAttribute("aria-invalid", "true");
    expect(screen.getByRole("alert")).toHaveTextContent("Niepoprawny");
  });

  it("Dialog renderuje tytuł i przycisk zamknięcia", () => {
    render(
      <Dialog otwarty onZmianaOtwarcia={() => {}} tytul="Tytuł" etykietaZamkniecia="Zamknij">
        treść
      </Dialog>,
    );
    expect(screen.getByRole("dialog", { name: "Tytuł" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Zamknij" })).toBeInTheDocument();
  });
});
