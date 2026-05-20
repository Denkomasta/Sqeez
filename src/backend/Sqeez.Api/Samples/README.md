# Sqeez CSV samples

This directory contains CSV examples that can be used with Sqeez import and
export features.

## Admin master import format

The file `admin-master-import.csv` demonstrates the CSV format used by the
administrator import. It can create school classes, subjects, and student
accounts in one upload.

Each CSV row represents one student record in the context of a class and,
optionally, a subject. Repeated class names and subject codes are processed as
the same class or subject, so the sample intentionally repeats some values.

### Columns

| Column | Required | Description |
| --- | --- | --- |
| `Class Name` | Yes | Name of the school class. |
| `Academic Year` | No | Optional academic year label. |
| `Subject Name` | No | Name of the subject to create. |
| `Subject Code` | No | Unique subject code used to group subject rows. |
| `First Name` | Yes | Student first name. |
| `Last Name` | Yes | Student last name. |
| `Email` | Yes | Student e-mail address. Also used as the basis for the username. |
| `Password` | No | Initial student password. If omitted, the system uses its default import password. |

### Endpoint

- Admin master import: `POST /api/import/master`

## Quiz import/export format

The file `quiz-import-all-question-types.csv` demonstrates the quiz CSV format.
The same format is used for both importing a quiz into a subject and exporting
an existing quiz from the quiz editor.

Each CSV row represents one answer option. Rows that share the same quiz title
belong to the same quiz, and rows with the same question order belong to the
same question.

### Columns

| Column | Required | Description |
| --- | --- | --- |
| `Quiz Title` | Yes | Name of the imported quiz. Rows with the same value are grouped into one quiz. |
| `Quiz Description` | No | Optional quiz description. Must be consistent for all rows of the same quiz. |
| `Max Retries` | No | Maximum number of attempts allowed for the quiz. Use `0` for no retries. |
| `Publish Date` | No | Optional UTC ISO 8601 date ending with `Z`, for example `2026-05-20T08:00:00Z`. |
| `Closing Date` | No | Optional UTC ISO 8601 date ending with `Z`. |
| `Question Order` | Yes | Numeric order of the question inside the quiz. |
| `Question Title` | Yes | Text of the question. |
| `Difficulty` | Yes | Question difficulty used for scoring and quiz metadata. |
| `Time Limit` | Yes | Time limit in seconds. Use `0` for no time limit. |
| `Has Penalty` | No | `True` if wrong answers should apply a penalty, otherwise `False`. |
| `Is Strict Multiple Choice` | No | `True` when all correct options must be selected exactly. |
| `Option Order` | Yes | Numeric order of the option inside the question. |
| `Option Text` | No | Text of the answer option. For free-text questions, this stores the suggested solution. |
| `Is Correct` | Yes | `True` if the option is correct. |
| `Is Free Text` | No | `True` for a free-text question row. |

### Question type rules

- A choice question must contain at least two options.
- A choice question must contain at least one correct option.
- A strict multiple-choice question is marked with `Is Strict Multiple Choice = True`.
- A free-text question must contain exactly one row.
- The free-text row must have `Is Free Text = True`, `Is Correct = True`, and a non-empty `Option Text`.
- Quiz-level metadata must be the same for all rows of the same quiz.
- Question-level metadata must be the same for all rows of the same question.

### Endpoints

- Quiz import: `POST /api/subjects/{subjectId}/quizzes/import`
- Quiz export: `GET /api/quizzes/{quizId}/export`
