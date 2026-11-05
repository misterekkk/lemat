// @Role: Publiczne API modułu solutions (przesłanie zdjęć kartki, ekran Rozwiązania, lista Rozwiązań) | @DependsOn: features/solutions/* | @UsedBy: app/(student)/exercises/[exerciseId]/page, app/(student)/solutions/page, app/(student)/solutions/[solutionId]/page | @Task: TASK-136, TASK-137
export { PhotoUpload } from "./PhotoUpload";
export { SolutionList } from "./SolutionList";
export { SolutionView } from "./SolutionView";
