using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using FactorialApp.Models;
using System.IO;

namespace FactorialApp.Services;

public sealed class AuthService
{
    private readonly DatabaseService _db;
    public AuthService(DatabaseService db) { _db = db; EnsureDefaults(); }
    private void EnsureDefaults()
    {
        using var c = _db.Open(); using var count = c.CreateCommand(); count.CommandText = "SELECT COUNT(*) FROM Users";
        if ((long)(count.ExecuteScalar() ?? 0L) > 0) return;
        Create("operator", "Operator123!", UserRole.Operator); Create("engineer", "Engineer123!", UserRole.Engineer); Create("admin", "Admin123!", UserRole.Admin);
    }
    public UserAccount? Authenticate(string user, string password)
    {
        using var c = _db.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT Id,Username,PasswordHash,Salt,Role FROM Users WHERE Username=$u"; cmd.Parameters.AddWithValue("$u", user);
        using var r = cmd.ExecuteReader(); if (!r.Read()) return null;
        var acc = new UserAccount { Id=r.GetInt32(0), Username=r.GetString(1), PasswordHash=r.GetString(2), Salt=r.GetString(3), Role=(UserRole)r.GetInt32(4) };
        return Verify(password, acc.Salt, acc.PasswordHash) ? acc : null;
    }
    public void Create(string user, string password, UserRole role)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16); byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120000, HashAlgorithmName.SHA256, 32);
        using var c = _db.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "INSERT OR IGNORE INTO Users(Username,PasswordHash,Salt,Role) VALUES($u,$h,$s,$r)";
        cmd.Parameters.AddWithValue("$u",user); cmd.Parameters.AddWithValue("$h",Convert.ToBase64String(hash)); cmd.Parameters.AddWithValue("$s",Convert.ToBase64String(salt)); cmd.Parameters.AddWithValue("$r",(int)role); cmd.ExecuteNonQuery();
    }
    public void ChangePassword(int id, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16); byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120000, HashAlgorithmName.SHA256, 32);
        using var c=_db.Open(); using var cmd=c.CreateCommand(); cmd.CommandText="UPDATE Users SET PasswordHash=$h,Salt=$s WHERE Id=$id"; cmd.Parameters.AddWithValue("$h",Convert.ToBase64String(hash)); cmd.Parameters.AddWithValue("$s",Convert.ToBase64String(salt)); cmd.Parameters.AddWithValue("$id",id); cmd.ExecuteNonQuery();
    }
    private static bool Verify(string p,string s,string h) { byte[] salt=Convert.FromBase64String(s); byte[] expected=Convert.FromBase64String(h); byte[] actual=Rfc2898DeriveBytes.Pbkdf2(p,salt,120000,HashAlgorithmName.SHA256,32); return CryptographicOperations.FixedTimeEquals(actual,expected); }
}

public sealed class RecipeService
{
    private readonly DatabaseService _db; public RecipeService(DatabaseService db) { _db=db; EnsureDefault(); }
    private void EnsureDefault(){ if(GetLatest("DEFAULT") is null) SaveNewVersion(new RecipeRecord()); }
    public RecipeRecord SaveNewVersion(RecipeRecord recipe)
    {
        int next=1; using(var c=_db.Open()){using var q=c.CreateCommand();q.CommandText="SELECT COALESCE(MAX(Version),0)+1 FROM Recipes WHERE Name=$n";q.Parameters.AddWithValue("$n",recipe.Name);next=Convert.ToInt32(q.ExecuteScalar());}
        recipe.Version=next; recipe.CreatedAt=DateTime.Now; string json=JsonSerializer.Serialize(recipe);
        using(var c=_db.Open()){using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO Recipes(Name,Version,CreatedAt,Json) VALUES($n,$v,$t,$j)";cmd.Parameters.AddWithValue("$n",recipe.Name);cmd.Parameters.AddWithValue("$v",next);cmd.Parameters.AddWithValue("$t",recipe.CreatedAt.ToString("O"));cmd.Parameters.AddWithValue("$j",json);cmd.ExecuteNonQuery();}
        return recipe;
    }
    public RecipeRecord? GetLatest(string name)
    {
        using var c=_db.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT Json FROM Recipes WHERE Name=$n ORDER BY Version DESC LIMIT 1";cmd.Parameters.AddWithValue("$n",name);var o=cmd.ExecuteScalar() as string;return o is null?null:JsonSerializer.Deserialize<RecipeRecord>(o);
    }
    public List<RecipeRecord> GetLatestAll()
    {
        using var c=_db.Open();using var cmd=c.CreateCommand();cmd.CommandText=@"SELECT r.Json FROM Recipes r JOIN (SELECT Name,MAX(Version) V FROM Recipes GROUP BY Name) x ON x.Name=r.Name AND x.V=r.Version ORDER BY r.Name";
        using var rd=cmd.ExecuteReader();var list=new List<RecipeRecord>();while(rd.Read()){var r=JsonSerializer.Deserialize<RecipeRecord>(rd.GetString(0));if(r!=null)list.Add(r);}return list;
    }
}

public sealed class AlarmService
{
    private readonly DatabaseService _db; public AlarmService(DatabaseService db)=>_db=db;
    public AlarmItem Raise(string code,string message,string source="SYSTEM")
    {
        var a=new AlarmItem{Timestamp=DateTime.Now,Code=code,Message=message,Source=source};using var c=_db.Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO Alarms(Timestamp,Code,Message,Source,IsAcknowledged) VALUES($t,$c,$m,$s,0); SELECT last_insert_rowid();";cmd.Parameters.AddWithValue("$t",a.Timestamp.ToString("O"));cmd.Parameters.AddWithValue("$c",code);cmd.Parameters.AddWithValue("$m",message);cmd.Parameters.AddWithValue("$s",source);a.Id=(long)(cmd.ExecuteScalar()??0L);return a;
    }
    public void Acknowledge(long id){using var c=_db.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE Alarms SET IsAcknowledged=1 WHERE Id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
    public List<AlarmItem> GetRecent(int n=100){using var c=_db.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT Id,Timestamp,Code,Message,Source,IsAcknowledged FROM Alarms ORDER BY Id DESC LIMIT $n";cmd.Parameters.AddWithValue("$n",n);using var r=cmd.ExecuteReader();var l=new List<AlarmItem>();while(r.Read())l.Add(new AlarmItem{Id=r.GetInt64(0),Timestamp=DateTime.Parse(r.GetString(1)),Code=r.GetString(2),Message=r.GetString(3),Source=r.GetString(4),IsAcknowledged=r.GetInt32(5)!=0});return l;}
}

public sealed class HistoryService
{
    private readonly DatabaseService _db; private readonly string _archive;
    public HistoryService(DatabaseService db,string archive){_db=db;_archive=archive;}
    public string SaveImage(byte[] bytes,string product,string serial,bool annotated,string ext=".jpg")
    {
        string day=DateTime.Now.ToString("yyyyMMdd");string type=annotated?"Annotated":"Raw";string dir=Path.Combine(_archive,day,type);Directory.CreateDirectory(dir);
        string safe=string.Concat(product.Select(ch=>Path.GetInvalidFileNameChars().Contains(ch)?'_':ch));string file=$"{DateTime.Now:HHmmss_fff}_{safe}_{serial}_{type}{ext}";string path=Path.GetFullPath(Path.Combine(dir,file));File.WriteAllBytes(path,bytes);return path;
    }
    public long Save(InspectionRecord x)
    {
        using var c=_db.Open();using var cmd=c.CreateCommand();cmd.CommandText=@"INSERT INTO Inspections(SerialNumber,Timestamp,ProductCode,RecipeName,RecipeVersion,Passed,NgCode,NgReason,X,Y,Angle,Area,ExposureUs,Gain,Operator,ImagePath,RawImagePath,CycleId) VALUES($s,$t,$p,$r,$rv,$pass,$nc,$nr,$x,$y,$a,$ar,$e,$g,$o,$ip,$rp,$cid); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$s",x.SerialNumber);cmd.Parameters.AddWithValue("$t",x.Timestamp.ToString("O"));cmd.Parameters.AddWithValue("$p",x.ProductCode);cmd.Parameters.AddWithValue("$r",x.RecipeName);cmd.Parameters.AddWithValue("$rv",x.RecipeVersion);cmd.Parameters.AddWithValue("$pass",x.Passed?1:0);cmd.Parameters.AddWithValue("$nc",x.NgCode);cmd.Parameters.AddWithValue("$nr",x.NgReason);cmd.Parameters.AddWithValue("$x",x.X);cmd.Parameters.AddWithValue("$y",x.Y);cmd.Parameters.AddWithValue("$a",x.Angle);cmd.Parameters.AddWithValue("$ar",x.Area);cmd.Parameters.AddWithValue("$e",x.ExposureUs);cmd.Parameters.AddWithValue("$g",x.Gain);cmd.Parameters.AddWithValue("$o",x.Operator);cmd.Parameters.AddWithValue("$ip",x.ImagePath);cmd.Parameters.AddWithValue("$rp",x.RawImagePath);cmd.Parameters.AddWithValue("$cid",x.CycleId);return (long)(cmd.ExecuteScalar()??0L);
    }
    public List<InspectionRecord> Query(HistoryFilter f,int limit=1000)
    {
        using var c=_db.Open();using var cmd=c.CreateCommand();var where=new List<string>{"1=1"};
        if(f.From.HasValue){where.Add("Timestamp >= $from");cmd.Parameters.AddWithValue("$from",f.From.Value.ToString("O"));}if(f.To.HasValue){where.Add("Timestamp <= $to");cmd.Parameters.AddWithValue("$to",f.To.Value.ToString("O"));}
        if(!string.IsNullOrWhiteSpace(f.Product)){where.Add("ProductCode LIKE $p");cmd.Parameters.AddWithValue("$p","%"+f.Product+"%");}if(!string.IsNullOrWhiteSpace(f.Recipe)){where.Add("RecipeName LIKE $r");cmd.Parameters.AddWithValue("$r","%"+f.Recipe+"%");}if(!string.IsNullOrWhiteSpace(f.NgCode)){where.Add("NgCode LIKE $n");cmd.Parameters.AddWithValue("$n","%"+f.NgCode+"%");}if(!string.IsNullOrWhiteSpace(f.SerialNumber)){where.Add("SerialNumber LIKE $s");cmd.Parameters.AddWithValue("$s","%"+f.SerialNumber+"%");}if(f.Result=="PASS")where.Add("Passed=1");else if(f.Result=="NG")where.Add("Passed=0");
        cmd.CommandText=$"SELECT Id,SerialNumber,Timestamp,ProductCode,RecipeName,RecipeVersion,Passed,NgCode,NgReason,X,Y,Angle,Area,ExposureUs,Gain,Operator,ImagePath,RawImagePath,CycleId FROM Inspections WHERE {string.Join(" AND ",where)} ORDER BY Id DESC LIMIT $limit";cmd.Parameters.AddWithValue("$limit",limit);
        using var r=cmd.ExecuteReader();var l=new List<InspectionRecord>();while(r.Read())l.Add(new InspectionRecord{Id=r.GetInt64(0),SerialNumber=r.GetString(1),Timestamp=DateTime.Parse(r.GetString(2)),ProductCode=r.GetString(3),RecipeName=r.GetString(4),RecipeVersion=r.GetInt32(5),Passed=r.GetInt32(6)!=0,NgCode=r.GetString(7),NgReason=r.GetString(8),X=r.GetDouble(9),Y=r.GetDouble(10),Angle=r.GetDouble(11),Area=r.GetDouble(12),ExposureUs=r.GetDouble(13),Gain=r.GetDouble(14),Operator=r.GetString(15),ImagePath=r.GetString(16),RawImagePath=r.GetString(17),CycleId=r.GetInt64(18)});return l;
    }
    public ProductionStats GetStats(HistoryFilter f){var all=Query(f,100000);return new ProductionStats{Total=all.Count,Pass=all.Count(x=>x.Passed),Ng=all.Count(x=>!x.Passed)};}
}

public sealed class CsvExportService
{
    public void Export(string path,IEnumerable<InspectionRecord> rows)
    {
        using var w=new StreamWriter(path,false,new UTF8Encoding(true));w.WriteLine("Id,SerialNumber,Timestamp,ProductCode,Recipe,Version,Result,NgCode,NgReason,X,Y,Angle,Area,ExposureUs,Gain,Operator,ImagePath");
        foreach(var x in rows)w.WriteLine(string.Join(',',new[]{x.Id.ToString(),Q(x.SerialNumber),Q(x.Timestamp.ToString("O")),Q(x.ProductCode),Q(x.RecipeName),x.RecipeVersion.ToString(),x.Passed?"PASS":"NG",Q(x.NgCode),Q(x.NgReason),x.X.ToString("F3"),x.Y.ToString("F3"),x.Angle.ToString("F3"),x.Area.ToString("F3"),x.ExposureUs.ToString("F1"),x.Gain.ToString("F2"),Q(x.Operator),Q(x.ImagePath)}));
    }
    private static string Q(string s)=>'"'+(s??"").Replace("\"","\"\"")+'"';
}
