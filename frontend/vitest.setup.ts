// @Role: Setup testów Vitest: matchery jest-dom i sprzątanie DOM | @DependsOn: @testing-library/jest-dom | @UsedBy: vitest.config.ts | @Task: TASK-006
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => cleanup());
