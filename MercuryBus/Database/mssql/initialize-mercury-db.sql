IF (NOT EXISTS(
    SELECT name
    FROM sys.schemas
    WHERE name = 'mercury'))
BEGIN
    EXEC('CREATE SCHEMA [mercury]')
END

DROP TABLE IF EXISTS mercury.Messages;
DROP TABLE IF EXISTS mercury.ReceivedMessages;

CREATE TABLE mercury.Messages (
  Id VARCHAR(450) PRIMARY KEY,
  Destination NVARCHAR(1000) NOT NULL,
  Headers NVARCHAR(1000) NOT NULL,
  Payload NVARCHAR(MAX) NOT NULL,
  Published SMALLINT DEFAULT 0,
  CreationTime BIGINT
);

-- ADD default value expression for creation_time
ALTER TABLE mercury.Messages ADD DEFAULT DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE()) FOR CreationTime

CREATE INDEX Message_Published_IDX ON mercury.Messages(Published, Id);

CREATE TABLE mercury.ReceivedMessages (
  ConsumerId VARCHAR(450),
  MessageId VARCHAR(450),
  PRIMARY KEY(ConsumerId, MessageId),
  CreationTime BIGINT
);

-- ADD default value expression for creation_time
ALTER TABLE mercury.ReceivedMessages ADD DEFAULT DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE()) FOR CreationTime
