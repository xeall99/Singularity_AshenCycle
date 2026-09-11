using SQLite;

[Table("enemies")]
public class EnemyData
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("battle_index")]
    public int BattleIndex { get; set; }

    [Column("max_hp")]
    public int MaxHP { get; set; }

    [Column("attack_damage")]
    public int AttackDamage { get; set; }

    [Column("reward_embers")]
    public int RewardEmbers { get; set; }

    [Column("is_boss")]
    public int IsBossValue { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("asset_key")]
    public string AssetKey { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }

    [Ignore]
    public bool IsBoss
    {
        get
        {
            return IsBossValue == 1;
        }
    }
}