// @Role: Test komunikatów: klucze używane w kodzie istnieją w pl-PL.json, wartości niepuste | @DependsOn: messages/pl-PL.json | @UsedBy: vitest | @Task: TASK-006
import { describe, expect, it } from "vitest";
import komunikaty from "../messages/pl-PL.json";

describe("messages/pl-PL.json", () => {
  it("zawiera teksty strony głównej", () => {
    expect(komunikaty.Strona.naglowek).not.toBe("");
    expect(komunikaty.Strona.opis).toMatch(/matur/);
  });
});
