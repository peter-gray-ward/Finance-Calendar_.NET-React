-- Create the User table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" UUID PRIMARY KEY,
    "UserName" VARCHAR(255) NOT NULL,
    "CheckingBalance" DECIMAL NOT NULL,
    "Password" VARCHAR(255) NOT NULL,
    "ProfileThumbnailBase64" TEXT
);

-- Create the Event table
CREATE TABLE IF NOT EXISTS "Events" (
    "Id" UUID PRIMARY KEY,
    "RecurrenceId" UUID,
    "Summary" VARCHAR(255),
    "Date" TIMESTAMP NOT NULL,
    "RecurrenceEndDate" TIMESTAMP,
    "Amount" DOUBLE PRECISION,
    "Total" DOUBLE PRECISION,
    "Balance" DOUBLE PRECISION,
    "Exclude" BOOLEAN,
    "Frequency" VARCHAR(255),
    "UserId" UUID,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);

-- Create the Expense table
CREATE TABLE IF NOT EXISTS "Expenses" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(255),
    "Amount" DOUBLE PRECISION,
    "StartDate" TIMESTAMP NOT NULL,
    "RecurrenceEndDate" TIMESTAMP,
    "Frequency" VARCHAR(255),
    "UserId" UUID,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);