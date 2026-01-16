# Functional Requirements - FunWasHad Application

**Version:** 1.0  \
**Last Updated:** 2026-01-13  \
**Application:** FunWasHad - Location-Based Activity Tracking & Business Marketing Platform

---

## 1. Overview

FunWasHad is a mobile application that combines location-based activity tracking with business marketing features. The application detects when users arrive at a new address, runs a guided workflow for that location, and surfaces subscribed business marketing content (themes, coupons, menus, and news). Users can also submit feedback to businesses, optionally with media attachments.

---

## 2. Functional Requirements (Business Rules)

### 2.1 Location Tracking and Address Change

#### FR-LOC-001: Monitor device location
The system SHALL monitor the device’s location while tracking is enabled.

#### FR-LOC-002: Detect arrival before triggering address change
The system SHALL only attempt address-change evaluation after the user is detected as stationary for a minimum period.

#### FR-LOC-003: Raise address-change event
When the detected address differs from the previously recorded address, the system SHALL raise a `NewLocationAddress` event.

---

### 2.2 Location-Based Workflow

#### FR-WF-001: Start location workflow on address change
When a `NewLocationAddress` event occurs, the system SHALL start a workflow using the `new-location.puml` definition.

#### FR-WF-002: Persist workflow state per address
The system SHALL persist workflow state locally using an address-based key so a workflow can be resumed later.

#### FR-WF-003: Resume workflow within 24 hours
If a workflow exists for the same address created within the last 24 hours, the system SHALL resume the existing workflow instance; otherwise it SHALL create a new workflow instance.

#### FR-WF-004: Address-based workflow identifier
The workflow identifier SHALL be deterministic for an address (for example `location:{address_hash}`) so that lookups for prior state are stable.

---

### 2.3 Business Discovery and Marketing Content

#### FR-BIZ-001: Find nearby businesses
The system SHALL allow querying for nearby businesses using latitude/longitude and a radius.

#### FR-BIZ-002: Return complete marketing payload
The system SHALL provide an endpoint to retrieve a business’s complete marketing payload, including its theme, active coupons, available menu items, and recent news.

#### FR-BIZ-003: Theme activation
The system SHALL treat a business theme as displayable only when the theme is marked active.

#### FR-BIZ-004: Coupon eligibility
The system SHALL treat a coupon as displayable only when:
- the coupon is marked active, and
- the current time is within the coupon validity window (`ValidFrom` to `ValidUntil`).

#### FR-BIZ-005: Menu item availability
The system SHALL treat a menu item as displayable only when the menu item is marked available.

#### FR-BIZ-006: News recency
The system SHALL allow retrieving a limited number of recent news items for a business.

---

### 2.4 Feedback and Attachments

#### FR-FB-001: Submit feedback
The system SHALL allow users to submit feedback for a business.

#### FR-FB-002: Required feedback fields
When submitting feedback, the system SHALL require:
- `BusinessId`
- `UserId`
- `FeedbackType`
- `Subject`
- `Message`

#### FR-FB-003: Allowed feedback types
The system SHALL accept `FeedbackType` values representing at least: review, complaint, suggestion, and compliment.

#### FR-FB-004: Rating bounds
If a rating is provided, the system SHALL only accept ratings in the range 1–5.

#### FR-FB-005: Public feedback flag
The system SHALL support marking feedback as public or private.

#### FR-FB-006: Moderation for public feedback
Public feedback SHALL be subject to approval before it is eligible for public display.

#### FR-FB-007: Business response
The system SHALL support storing a business response to a feedback submission with a response timestamp.

#### FR-FB-008: Upload feedback attachments
The system SHALL allow users to upload image and video attachments associated with a feedback submission.

#### FR-FB-009: Attachment metadata
For each feedback attachment, the system SHALL store:
- attachment type (image or video)
- file name
- content type
- storage URL
- optional thumbnail URL
- optional file size
- optional duration (for video)

---

## 3. Data Integrity Rules

#### FR-DATA-001: Required business fields
Business records SHALL include a name and address.

#### FR-DATA-002: Cascading deletes
When a business is deleted, the system SHALL delete dependent marketing and feedback data associated with that business.

