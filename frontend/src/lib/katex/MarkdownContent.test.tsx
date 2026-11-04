// @Role: Testy TrescMarkdown: wzory przez KaTeX i brak wykonywania HTML z wejścia | @DependsOn: MarkdownContent.tsx, Testing Library | @UsedBy: vitest | @Task: TASK-006
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { TrescMarkdown } from "./MarkdownContent";

describe("TrescMarkdown", () => {
  it("renderuje wzór $x^2+1$ z klasą katex", () => {
    const { container } = render(<TrescMarkdown tresc={"Oblicz $x^2+1$ dla x=2"} />);
    expect(container.querySelector(".katex")).not.toBeNull();
  });

  it("nie wykonuje HTML z wejścia", () => {
    const { container } = render(
      <TrescMarkdown
        tresc={'<script>window.__xss=1</script><img src=x onerror="window.__xss=1">tekst'}
      />,
    );
    expect(container.querySelector("script")).toBeNull();
    expect(container.querySelector("img")).toBeNull();
    expect((window as unknown as { __xss?: number }).__xss).toBeUndefined();
  });
});
