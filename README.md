\# RaceDay-POE



\*\*Module:\*\* PROG6212 – Programming 2B

\*\*Student:\*\* LERATO ROSELINE MABUSELA – ST10494431



RaceDay is a web-based event management system for the South African road running, walking and cycling community. Organisers create and manage events, categories and results. Participants browse events, enrol in categories and track their personal performance.



\- \[Part 2 – RESTful API Development](#part-2--restful-api-development)

\- \[Part 1 – System Planning and Database Design](#part-1--system-planning-and-database-design)



\---



\# Part 2 – RESTful API Development



A RESTful ASP.NET Core Web API that powers the RaceDay system, built to the endpoint plan from Part 1.



\*\*YouTube video link:\*\* \ [ https://youtu.be/0bCXeCDCm1s?si=z9I2UtbG57IKm9dc]



\## Tech stack

\- ASP.NET Core 8 Web API (C#)

\- Entity Framework Core 8 (Code-First) with SQL Server

\- Server-side session authentication (cookie) with BCrypt password hashing

\- Swagger (Swashbuckle) for live endpoint documentation

\- xUnit unit tests with the EF Core in-memory provider

\- GitHub Actions CI



\## Project structure

```

RaceDay-POE/

├── .github/workflows/      CI workflows

├── docs/                   Part 1 documents and CI screenshots

└── Part 2/RaceDayApi/

&#x20;   ├── RaceDayApi.sln

&#x20;   ├── RaceDayApi/         the Web API (Auth, Controllers, Data, Dtos, Migrations, Models)

&#x20;   └── RaceDayApi.Tests/   unit tests

```



\## The two roles

\*\*Organiser\*\*

\- Creates, updates and deletes their own events

\- Adds, edits and deletes categories on their own events

\- Views all enrolments for their own events

\- Captures finish times and positions



\*\*Participant\*\*

\- Views events, categories and public results

\- Enrols in a category and cancels their own enrolment

\- Views their own enrolments and results



Users choose their role when they register. Organiser features are blocked for Participants, and Participant features are blocked for Organisers. Users can only view or change their own data.



\## Setup and run



\*\*Requirements:\*\* .NET 8 SDK, SQL Server (LocalDB or Express), and the EF tool: `dotnet tool install --global dotnet-ef`



1\. Clone the repository.

2\. Open `Part 2/RaceDayApi/RaceDayApi/appsettings.json` and set the connection string to your SQL Server, for example:

```

"ConnectionStrings": {

&#x20; "DefaultConnection": "Server=(localdb)\\\\MSSQLLocalDB;Database=RaceDay;Trusted\_Connection=True;TrustServerCertificate=True;"

}

```

3\. Create the database from the migration:

```

cd "Part 2/RaceDayApi"

dotnet ef database update --project RaceDayApi

```

4\. Run the API:

```

dotnet run --project RaceDayApi --launch-profile https

```

5\. Open Swagger at `https://localhost:7033/swagger`.



\## Using the API in Swagger

1\. Register a user with `POST /api/auth/register`.

2\. Log in with `POST /api/auth/login`. The API starts a session and your browser stores the session cookie.

3\. Try any endpoint. Calls now run as that user and role.

4\. Call `POST /api/auth/logout` before logging in as the other role.



Not logged in returns \*\*401\*\*. Logged in with the wrong role, or touching someone else's data, returns \*\*403\*\*.



\## Endpoints

| Method | Route | Access |

|---|---|---|

| POST | /api/auth/register | Public |

| POST | /api/auth/login | Public |

| POST | /api/auth/logout | Any logged-in user |

| GET | /api/users/{id} | Own profile only |

| PUT | /api/users/{id} | Own profile only |

| GET | /api/events | Public |

| GET | /api/events/{id} | Public |

| POST | /api/events | Organiser |

| PUT | /api/events/{id} | Organiser (own event) |

| DELETE | /api/events/{id} | Organiser (own event) |

| GET | /api/events/{eventId}/categories | Public |

| POST | /api/events/{eventId}/categories | Organiser (own event) |

| PUT | /api/categories/{id} | Organiser (own event) |

| DELETE | /api/categories/{id} | Organiser (own event) |

| POST | /api/categories/{id}/enrol | Participant |

| GET | /api/participants/{id}/enrolments | Participant (own) |

| GET | /api/events/{id}/enrolments | Organiser (own event) |

| DELETE | /api/enrolments/{id} | Participant (own) |

| POST | /api/enrolments/{id}/results | Organiser (own event) |

| GET | /api/participants/{id}/results | Participant (own) |

| GET | /api/events/{id}/results | Public |



Every endpoint is described in Swagger, including its purpose, expected input and responses.



\## Design notes

\- \*\*Sessions, not tokens:\*\* the Part 1 plan mentioned a token on login. The Part 2 brief requires session management, so login stores the user ID and role in a server-side session.

\- \*\*Role enforcement:\*\* a custom `SessionAuthorize` attribute returns 401 or 403. Organisers can only change their own events, categories and results.

\- \*\*Passwords\*\* are hashed with BCrypt and never stored in plain text.

\- \*\*Enrolments:\*\* duplicate enrolment and full categories return 409. Cancelling marks the enrolment as `Cancelled` rather than deleting it.

\- \*\*Schema:\*\* the database matches the Part 1 ERD, with an added `EventType` column (Run, Walk or Cycle) on Event.



\## Unit tests

```

cd "Part 2/RaceDayApi"

dotnet test

```

The tests cover registration and login, event management, role-based access (401 and 403), and event enrolments. Each test covers success and failure cases, and uses a fresh in-memory database.



\## CI/CD

The workflow `.github/workflows/build-and-test.yml` builds the solution and runs all unit tests on every push.







!\[Successful CI build](docs/ci-success-part2.png)







\---



\# Part 1 – System Planning and Database Design



\## System description

RaceDay is a full-stack web-based event management system built for the South African road running, walking and cycling community. The platform allows Event Organisers to create and manage events, categories and participant results, while Participants can browse upcoming events, enter events, track their personal performance history and prepare for race day.



This section covers Part 1 of the Portfolio of Evidence: system planning and database design, including the Entity Relationship Diagram, the API endpoint plan and the SQL database script.



\## Roles

\*\*Organiser\*\* – Can create, edit and delete events, manage event categories, capture participant results and view all event enrolments.



\*\*Participant\*\* – Can create an account, browse events, enter an event by selecting a category, view their own enrolments and track their personal results.



\## Part 1 files

All Part 1 documents are in the \[`docs`](docs) folder:

\- Entity Relationship Diagram

\- RaceDay API Endpoint Plan (PDF)

\- `RaceDay\_Database\_Script.sql`



\## Technologies used

\- Microsoft SQL Server (SSMS) for database design and scripting

\- draw.io for the Entity Relationship Diagram

\- GitHub Actions for CI/CD validation

\- Microsoft Word for the API endpoint plan documentation



\## Prerequisites

\- SQL Server Management Studio (SSMS) installed

\- A running SQL Server instance (local or SQLEXPRESS)

\- Git installed for cloning the repository



\*\*Part 1 video:\*\* \[https://youtu.be/HTAxVgrVB0g?si=ljazX0ObMBkD3X\_j]





