# EF Core / .NET DB հրամանների ուղեցույց

Բոլ հրամանները գործարկվում են `backend/` թղթապանակից։

---

## 0. Գործիքի տեղադրում (մեկ անգամ)

```bash
dotnet tool install --global dotnet-ef      # տեղադրել dotnet ef գործիքը
dotnet tool update  --global dotnet-ef      # թարմացնել
dotnet ef --version                         # ստուգել՝ տեղադրված է՞
```

---

## 1. Migration-ներ (schema-ի փոփոխություններ)

### Նոր migration ստեղծել
```bash
dotnet ef migrations add <Անուն>
```
Համեմատում է C#-ի model-ը վերջին migration-ի հետ, գեներացնում է նոր ֆայլ
`Migrations/<timestamp>_<Անուն>.cs` (`Up()` = կիրառել, `Down()` = հետ գցել)։
**Բազան չի փոխվում** — միայն ֆայլ է գեներացվում։
Օրինակ՝ `dotnet ef migrations add AddSessions`

### Վերջին migration-ը ջնջել (եթ դեռ չի կիրառվել բազայի վրա)
```bash
dotnet ef migrations remove
```
Ջնջում է վերջին migration ֆայլը ու հետ բերում snapshot-ը։
Եթ արդեն կիրառված է բազայում — նախ պիտի `database update <նախորդ migration>` անել։

### Migration-ների ցուցակ
```bash
dotnet ef migrations list
```
Ցույ է տալիս բոլ migration-ները ու որոնք են արդեն կիրառված բազայում։

### Migration-ը SQL-ի վերածել (կիրառելու փոխարեն տեսնել)
```bash
dotnet ef migrations script                 # ամբողջ SQL
dotnet ef migrations script <From> <To>     # միայն միջակայքը
```

---

## 2. Բազան թարմացնել (schema-ն կիրառել)

### Բոլ չկիրառված migration-ները կիրառել
```bash
dotnet ef database update
```
Միանում է բազային, նայում `__EFMigrationsHistory` աղյուսակին, գործարկում
չկիրառված migration-ների `Up()`-ը (իրական `CREATE TABLE` և այլն)։

### Կոնկրետ migration-ի կիրառել / հետ գցել
```bash
dotnet ef database update <MigrationName>
```
Եթ target-ը ընթացիկից առաջ է → գործարկում է `Down()`-երը (rollback)։
Օրինակ՝ `dotnet ef database update InitialCreate` → հետ է գցում `AddSessions`-ը։

---

## 3. Բազան reset անել (prisma migrate reset-ի համարժեք)

### Schema-ն ամբողջությամբ վերստեղծել, տվյալները ջնջել, բազան չdrop անել
```bash
dotnet ef database update 0     # բոլ Down()-երը → schema-ն դատարկվում է
dotnet ef database update       # բոլ Up()-երը → schema-ն նորից, դատարկ
```
`0` = «առաջին migration-ից առաջ» հատուկ target։

### Ամբողջ բազան drop անել ու վերստեղծել
```bash
dotnet ef database drop -f      # -f = առանց հարցնելու
dotnet ef database update
```
Neon-ը երբեմն բողոքում է «cannot drop the currently open database» — այդ դեպքում
օգտագործիր `update 0` տարբերակը կամ Neon web console-ը։

### Միայն տվյալները ջնջել (աղյուսակները թողնել)
```bash
psql "<connection string>" -c 'TRUNCATE "Sessions", "Users" RESTART IDENTITY CASCADE;'
```
`RESTART IDENTITY` = Id հաշվիչը վերադառնում է 1-ի։
`CASCADE` = կապված աղյուսակներն էլ մաքրում է (FK)։
Connection string-ը վերցնել՝ `dotnet user-secrets list | grep ConnectionStrings`

---

## 4. DbContext-ի ինֆո

```bash
dotnet ef dbcontext info                    # ընթացիկ context-ի կարգավորումները
dotnet ef dbcontext list                    # բոլ DbContext class-երը project-ում
dotnet ef dbcontext scaffold ...            # գոյություն ունեցող բազայից model գեներացնել (reverse)
```

---

## 5. User Secrets (գաղտնիքներ, repo-ից դուrs)

```bash
dotnet user-secrets init                    # <UserSecretsId> ավելացնում է .csproj-ում (մեկ անգամ)
dotnet user-secrets set "Key:Sub" "value"   # գաղտնիք ավելացնել/թարմացնել
dotnet user-secrets list                    # բոլ գաղտնիքները
dotnet user-secrets remove "Key:Sub"        # մեկ գաղտնիք հանել
dotnet user-secrets clear                   # բոլ գաղտնիքները ջնջել
```
Ֆայլը՝ `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` (միայն Development-ում է կարդացվում)։

Ներդրված key՝ JSON-ի `{ "Jwt": { "Key": "..." } }` = `"Jwt:Key"`։
Environment variable-ում՝ `:` → `__` (`Jwt__Key`)։

---

## 6. Build / Run

```bash
dotnet build                                # կոմպիլացնել
dotnet run                                  # գործարկել
dotnet run --project backend                # այլ թղթապանակից
dotnet watch run                            # auto-reload փոփոխության դեպքում
```

---

## Typical workflow — նոր աղյուսակ/դաշտ ավելացնելիս

```bash
# 1. C#-ում փոխիր model-ը (նոր class կամ նոր property) + AppDbContext-ում DbSet
# 2. Migration գեներացնել
dotnet ef migrations add AddSomething
# 3. Ստուգել գեներացված ֆայլը (Migrations/*_AddSomething.cs)
# 4. Կիրառել բազայի վրա
dotnet ef database update
# 5. Սխալ ա՞ եղել — հետ գցել
dotnet ef database update <PreviousMigration>
dotnet ef migrations remove
```

---

## Ծանոթագրություններ EF-ի աշխատանքի մասին

- **`Add` / `Remove` / `Update`** — միայն նշում են change tracker-ում («Added», «Deleted»...)։ Իրական SQL-ը գնում է `SaveChanges()` / `SaveChangesAsync()`-ի ժամանակ։
- **`Remove(entity)`** — ուզում է իրական entity օբյեկտ, ոչ `int` id։ Նախ `FindAsync(id)` / `FirstOrDefaultAsync(...)`, հետո `Remove(user)`։
- **`ExecuteDeleteAsync()` / `ExecuteUpdateAsync()`** (EF Core 7+) — ուղիղ SQL, առանց օբյեկտ բեռնելու, առանց tracking — ավելի արագ bulk գործողությունների համ։
- **`FirstOrDefaultAsync`** — չգտնելու դեպքում `null`, exception չի նետում։ `FirstAsync` — նետում է։
- **Migration-ի անունը** նկարագրական դարձրու (`AddSessions`, `AddUserBio`) — git log-ի պես կարդացվում է։
