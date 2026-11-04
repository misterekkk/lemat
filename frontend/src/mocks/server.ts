// @Role: Serwer MSW dla Node (Vitest, Playwright): ten sam zestaw handlerów co w przeglądarce | @DependsOn: msw/node, mocks/handlers | @UsedBy: testy Vitest (*.test.ts), e2e (TASK-139) | @Task: TASK-132
import { setupServer } from "msw/node";
import { handlers } from "./handlers";

export const server = setupServer(...handlers);
