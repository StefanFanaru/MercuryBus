IF
    (NOT EXISTS(
            SELECT name
            FROM sys.schemas
            WHERE name = '$(TRAM_SCHEMA)'))
    BEGIN
        EXEC ('CREATE SCHEMA [$(TRAM_SCHEMA)]')
    END

DROP TABLE IF EXISTS $(TRAM_SCHEMA).Messages;
DROP TABLE IF EXISTS $(TRAM_SCHEMA).ReceivedMessages;

CREATE TABLE $(TRAM_SCHEMA).Messages
(
    Id           VARCHAR(450) PRIMARY KEY,
    Destination  NVARCHAR(1000) NOT NULL,
    Headers      NVARCHAR(1000) NOT NULL,
    Payload      NVARCHAR(MAX)  NOT NULL,
    Published    SMALLINT DEFAULT 0,
    CreationTime BIGINT
);

-- ADD default value expression for creation_time
ALTER TABLE $(TRAM_SCHEMA).Messages
    ADD DEFAULT DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE()) FOR CreationTime

CREATE INDEX Message_Published_IDX ON $(TRAM_SCHEMA).Messages (Published, Id);

CREATE TABLE $(TRAM_SCHEMA).ReceivedMessages
(
    ConsumerId   VARCHAR(450),
    MessageId    VARCHAR(450),
    PRIMARY KEY (ConsumerId, MessageId),
    CreationTime BIGINT
);

-- ADD default value expression for creation_time
ALTER TABLE $(TRAM_SCHEMA).ReceivedMessages
    ADD DEFAULT DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE()) FOR CreationTime
