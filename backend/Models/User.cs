namespace Backend.Models;

public class User
{
    public int Id { get; set; }
    public int Score { get; set; } = 0;
    public int ResetCodeAttempts { get; set; } = 0;

    public string   Email {get; set;} = "";
    public string   Username {get; set;} = "";
    public string?  Password {get; set;}
    public string?  NewPassword {get; set; }
    public string?  Provider {get; set;}
    public string?  ProviderId {get; set;}
    public string?  Color {get; set;}
    public string   Language {get; set;} = "en";
    public string   Avatar {get; set;} = "default.png";
    public string?  ResetCode {get; set;}
    public Role     Role {get; set;} = Role.PLAYER;

    public bool     IsBot { get; set;} = false;
    public bool     Theme { get; set; } = true;
    public DateTime? TermsAcceptedAt {get; set;}
    public DateTime? CodeExpire {get; set;}
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;
}

// model Users {
//   id                Int               @id @default(autoincrement())
//   Email             String            @unique
//   Password          String?
//   provider          String?
//   providerId        String?
//   Username          String
//   role              Role              @default(PLAYER)
//   language          String            @default("en")
//   theme             Boolean           @default(true)
//   color             String?
//   avatar            String            @default("default.png")
//   isBot             Boolean           @default(false)
//   score             Int               @default(0)
//   resetCode         String?
//   codeExpire        DateTime?
//   resetCodeAttempts Int               @default(0)
//   termsAcceptedAt   DateTime?
//   createdAt         DateTime          @default(now())
//   updatedAt         DateTime          @updatedAt
//   gameRooms         GameRoom          []
//   roomsUsers        RoomUser          []
//   sessions          Sessions          []
//   history           UserStats?
//   sentRequests      FriendsRequest    [] @relation("SentRequests")
//   receivedRequests  FriendsRequest    [] @relation("ReceivedRequests")
//   gameParticipants	GameParticipants  []
// 	gamesWon			    GameResults       [] @relation("Winner")

//   @@unique([provider, providerId])
// }