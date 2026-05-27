namespace GachaSystem
{
    using System;

    /// <summary>
    /// İstemci tarafında Gacha/Drop animasyonu için kullanılan ödül yapısı.
    /// Kullanıcının istediği özel alanları (SkinId, CardId vb.) destekler.
    /// </summary>
    public class GachaReward
    {
        public int Count;
        public int DataId;
        public int Type;

        public GachaReward() { }

        public GachaReward(RewardItem item)
        {
            Count = item.Count;
            DataId = item.DataId;
            Type = MapToGachaType(item.Type);

            // Mapping to specialized fields
            switch (item.Type)
            {
                case ItemType.Avatar:
                case ItemType.Skin:
                    DataId = item.DataId; // SkinId olarak da kullanılabilir
                    break;
                case ItemType.Character:
                    DataId = item.DataId;
                    break;
                case ItemType.Emote:
                    DataId = item.DataId;
                    break;
            }
        }

        private int MapToGachaType(ItemType type)
        {
            // Kullanıcının paylaştığı kod örneğindeki tiplere göre eşleştirme:
            // 1: Hero, 2: TokenDoubler, 6: PowerPoints, 7: Gold, 8: Gems, 9: Skin, 10: Emote
            switch (type)
            {
                case ItemType.Character: return 7;
                case ItemType.XPBoost: return 4;
                case ItemType.PowerPoints: return 8;
                case ItemType.Coins: return 1;
                case ItemType.Gems: return 0;
                case ItemType.Avatar: return 3;
                case ItemType.Skin: return 11;
                case ItemType.Emote: return 9;
                case ItemType.BattlePass : return 2;
                case ItemType.TrophyShield: return 5;
                case ItemType.StarterBundle: return 6;
                case ItemType.GachaBox: return 10;
                default: return 0;
            }
        }
    }
}
