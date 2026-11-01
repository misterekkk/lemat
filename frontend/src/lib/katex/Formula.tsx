// @Role: Komponent Wzor: renderuje wzór LaTeX przez KaTeX (throwOnError=false, trust=false) | @DependsOn: katex | @UsedBy: TrescMarkdown, ekrany Zadań i Transkrypcji (planowane TASK-082+) | @Task: TASK-006
import katex from "katex";

export interface WzorProps {
  latex: string;
  blokowy?: boolean;
  className?: string;
}

export function Wzor({ latex, blokowy = false, className }: WzorProps) {
  const html = katex.renderToString(latex, {
    displayMode: blokowy,
    throwOnError: false,
    trust: false,
    maxExpand: 1000,
  });
  const Znacznik = blokowy ? "div" : "span";
  return <Znacznik className={className} dangerouslySetInnerHTML={{ __html: html }} />;
}
