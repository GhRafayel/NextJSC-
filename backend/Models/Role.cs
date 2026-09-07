namespace Backend.Models;

public enum Role
{
    PLAYER,
    ADMIN,
    BOT
}

public enum RoomType
{
    PUBLIC,
    PRIVATE,
}

public enum RommStatus
{
    WAITING,
    PLAYING,
    STARTING,
    FINISHED,
}
