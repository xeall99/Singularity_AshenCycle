using SQLite;

[Table("skills")]
public class SkillData
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("mana_cost")]
    public int ManaCost { get; set; }

    [Column("power")]
    public int Power { get; set; }

    [Column("target_type")]
    public string TargetType { get; set; }

    [Column("effect_type")]
    public string EffectType { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("asset_key")]
    public string AssetKey { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }
}