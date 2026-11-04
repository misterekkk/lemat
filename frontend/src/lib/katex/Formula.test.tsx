// @Role: Testy komponentu Wzor (klasa katex, brak wyjątku dla błędnego LaTeX) | @DependsOn: Formula.tsx, Testing Library | @UsedBy: vitest | @Task: TASK-006
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Wzor } from "./Formula";

describe("Wzor", () => {
  it("renderuje x^2+1 z klasą katex", () => {
    const { container } = render(<Wzor latex="x^2+1" />);
    expect(container.querySelector(".katex")).not.toBeNull();
  });

  it("nie rzuca wyjątku dla błędnego LaTeX (throwOnError=false)", () => {
    expect(() => render(<Wzor latex="\\frac{" />)).not.toThrow();
  });
});
