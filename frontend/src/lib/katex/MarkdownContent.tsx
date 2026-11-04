// @Role: Komponent TrescMarkdown: Markdown z wzorami $...$ (react-markdown + remark-math + rehype-katex), bez wykonywania HTML z wejścia | @DependsOn: react-markdown, remark-math, rehype-katex | @UsedBy: ekrany Zadań, Wkładek i Wskazówek (planowane TASK-082+) | @Task: TASK-006
import ReactMarkdown from "react-markdown";
import rehypeKatex from "rehype-katex";
import remarkMath from "remark-math";

export interface TrescMarkdownProps {
  tresc: string;
  className?: string;
}

export function TrescMarkdown({ tresc, className }: TrescMarkdownProps) {
  return (
    <div className={className}>
      <ReactMarkdown
        remarkPlugins={[remarkMath]}
        rehypePlugins={[[rehypeKatex, { throwOnError: false, trust: false, maxExpand: 1000 }]]}
      >
        {tresc}
      </ReactMarkdown>
    </div>
  );
}
