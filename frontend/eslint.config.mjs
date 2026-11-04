// @Role: Konfiguracja ESLint 9 (flat): eslint-config-next, zakaz tekstów na sztywno w JSX, zgodność z Prettier | @DependsOn: eslint-config-next, eslint-config-prettier | @UsedBy: npm run lint | @Task: TASK-006, TASK-131
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";
import prettier from "eslint-config-prettier/flat";

const konfiguracja = [
  ...nextVitals,
  ...nextTs,
  prettier,
  {
    ignores: [".next/**", "node_modules/**", "coverage/**", "next-env.d.ts"],
  },
  {
    files: ["src/**/*.tsx"],
    ignores: ["src/**/*.test.tsx"],
    rules: {
      // NFR-10: wszystkie teksty UI pochodzą z messages/pl-PL.json
      "react/jsx-no-literals": ["error", { noStrings: true, ignoreProps: true }],
    },
  },
  {
    // TASK-131: fetch do /api/v1 wyłącznie w lib/api/client.ts
    files: ["src/**/*.{ts,tsx}"],
    ignores: ["src/lib/api/client.ts", "src/**/*.test.{ts,tsx}", "src/mocks/**"],
    rules: {
      "no-restricted-syntax": [
        "error",
        {
          selector: "CallExpression[callee.name='fetch']",
          message: "Wywołania HTTP tylko przez lib/api/client.ts.",
        },
      ],
    },
  },
];

export default konfiguracja;
