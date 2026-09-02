-- RaceDay Database Script
-- Author: Lee
-- Creates and seeds the RaceDay database schema

-- Organiser table
CREATE TABLE Organiser (
    OrganiserID     INT IDENTITY(1,1) PRIMARY KEY,
    FirstName       VARCHAR(50)  NOT NULL,
    LastName        VARCHAR(50)  NOT NULL,
    Email           VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    PhoneNumber     VARCHAR(20)  NULL,
    CreatedAt       DATETIME     NOT NULL DEFAULT GETDATE()
);

-- Participant table
CREATE TABLE Participant (
    ParticipantID   INT IDENTITY(1,1) PRIMARY KEY,
    FirstName       VARCHAR(50)  NOT NULL,
    LastName        VARCHAR(50)  NOT NULL,
    Email           VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    DateOfBirth     DATE         NULL,
    PhoneNumber     VARCHAR(20)  NULL,
    CreatedAt       DATETIME     NOT NULL DEFAULT GETDATE()
);

-- Event table
CREATE TABLE Event (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT          NOT NULL,
    EventName       VARCHAR(100) NOT NULL,
    EventDate       DATE         NOT NULL,
    Location        VARCHAR(150) NOT NULL,
    Description     VARCHAR(500) NULL,
    CreatedAt       DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES Organiser(OrganiserID)
);

-- Category table
CREATE TABLE Category (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT           NOT NULL,
    CategoryName    VARCHAR(50)   NOT NULL,
    Distance        DECIMAL(5,2)  NOT NULL,
    MaxParticipants INT           NOT NULL,
    EntryFee        DECIMAL(8,2)  NOT NULL DEFAULT 0,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventID)
        REFERENCES Event(EventID)
);

CREATE TABLE Enrolment (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT      NOT NULL,
    CategoryID      INT      NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20) NOT NULL DEFAULT 'Confirmed'
        CHECK (Status IN ('Confirmed', 'Cancelled')),
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantID)
        REFERENCES Participant(ParticipantID),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID)
);

CREATE TABLE Result (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT NOT NULL UNIQUE,
    FinishTime      TIME NOT NULL,
    Position        INT NULL,
    CapturedBy      INT NOT NULL,
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolment(EnrolmentID),
    CONSTRAINT FK_Result_Organiser FOREIGN KEY (CapturedBy)
        REFERENCES Organiser(OrganiserID)
);

INSERT INTO Organiser (FirstName, LastName, Email, PasswordHash, PhoneNumber)
VALUES
('Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za', 'hashed_pw_1', '0821234567'),
('Lindiwe', 'Nkosi', 'lindiwe.nkosi@raceday.co.za', 'hashed_pw_2', '0837654321');

INSERT INTO Participant (FirstName, LastName, Email, PasswordHash, DateOfBirth, PhoneNumber)
VALUES
('Sipho', 'Dlamini', 'sipho.dlamini@example.com', 'hashed_pw_3', '1995-03-14', '0712345678'),
('Anika', 'van der Merwe', 'anika.vdm@example.com', 'hashed_pw_4', '1998-07-22', '0723456789');


INSERT INTO Event (OrganiserID, EventName, EventDate, Location, Description)
VALUES
(1, 'Johannesburg City 10K', '2026-10-18', 'Johannesburg', 'A flat, fast 10K through the Johannesburg CBD.'),
(1, 'Cape Town Cycle Challenge', '2026-11-08', 'Cape Town', 'A scenic road cycling event around the Cape Peninsula.'),
(2, 'Durban Beachfront Fun Run', '2026-12-06', 'Durban', 'A family-friendly walk and run along the Durban beachfront.');

INSERT INTO Category (EventID, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES
(1, '10km', 10.00, 500, 150.00),
(1, '5km', 5.00, 300, 100.00),
(2, '109km Cycle', 109.00, 800, 350.00),
(2, '35km Cycle', 35.00, 400, 200.00),
(3, '5km Fun Run', 5.00, 600, 80.00),
(3, '2km Walk', 2.00, 400, 50.00);

INSERT INTO Enrolment (ParticipantID, CategoryID, Status)
VALUES
(1, 1, 'Confirmed'),   -- Sipho enters the 10km
(1, 3, 'Confirmed'),   -- Sipho also enters the 109km Cycle
(2, 5, 'Confirmed'),   -- Anika enters the 5km Fun Run
(2, 2, 'Cancelled');   -- Anika cancelled her 5km entry

INSERT INTO Result (EnrolmentID, FinishTime, Position, CapturedBy)
VALUES
(1, '00:42:15', 23, 1),   -- Sipho's 10km result, captured by Thabo
(2, '03:12:40', 8, 1),    -- Sipho's 109km Cycle result, captured by Thabo
(3, '00:28:50', 15, 2);   -- Anika's 5km Fun Run result, captured by Lindiwe