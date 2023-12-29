CREATE TABLE [dbo].[Client]
(
    [Username] NVARCHAR(50) NOT NULL,
    [RoomId] UNIQUEIDENTIFIER NOT NULL,
    [Role] TINYINT NULL,
    [ConnectionId] NVARCHAR(50) NOT NULL,
    PRIMARY KEY ([Username], [ConnectionId]),
    FOREIGN KEY ([RoomId]) REFERENCES Room(Id)
)
