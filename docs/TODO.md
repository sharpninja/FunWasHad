# TODO List

This document tracks pending tasks, improvements, and future work for the FunWasHad project.

## High Priority

## Medium Priority

- [ ] Perform code review

## Low Priority

## Future Enhancements

## Bug Fixes

## Documentation

## Testing

## Refactoring

- [x] Split multi-type files into single-type files following single-responsibility principle (2025-01-27)
  - Split WorkflowModels.cs, Payloads.cs, Types.cs, ChatEntry.cs, FeedbackModels.cs, BusinessModels.cs
  - Split LocationHandlers.cs and MarketingHandlers.cs into individual handler files
  - Extract TestWorkflowController to separate file
  - All files now follow one type per file pattern with matching file names

---

## Completed Tasks

- [x] Add XMLDOC to each unit test expressing what is being tested, what data is involved, why the data matters and the expected outcome and the reason it is expected (2025-01-27)
  - Completed: 45+ test files documented with comprehensive XMLDOC comments
  - All major test files completed including MarketingControllerTests.cs
  - All 195 tests passing successfully
- [x] Better categorize log messages with trace, debug, information, and error throughout the solution (2025-01-26)
- [x] Add button to scroll log viewer to the end (2025-01-26)
- [x] Add dropdown to log viewer to set log level displayed (2025-01-26)
- [x] Merge all depend-a-bot pull requests (2025-01-26)
- [x] Link to Pages from README (2025-01-26)
- [x] Cleanup docs folder. Remove summaries and plans (2025-01-26)
- [x] Ensure console logging is enabled in the android app (2025-01-26)
- [x] Configure cursor to skip executing $PROFILE on commands (2025-01-26)
- [x] When starting the application, initialize the location tracking with the current location (2025-01-26)
- [x] Update log viewer control to ensure the most recently added entry is visible immediately after being added (2025-01-26)
- [x] Add pause feature to log viewer and use icons instead of text for buttons (2025-01-26)
- [x] Add native map control to the location tracking user control and show device location on the map (2025-01-26)

---

*Last updated: 2025-01-27*
