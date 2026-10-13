using System.Collections.Generic;

namespace Mutiny.Levels
{
    public sealed class MutinyLevelData
    {
        public int Width;
        public int Height;
        public int Players;
        public string Name;

        // [y, x]，保持原版 XML 坐标：x 从左到右，y 从上到下。
        // 保留非空 tile 的原始名称；XML 空格标记 "-" 解析为 null。
        // 不在数据层转换 Unity 坐标。
        public string[,] Terrain;
        public string[,] Background;

        public List<MutinyLevelObject> Objects = new();
    }

    public sealed class MutinyLevelObject
    {
        public string Type;
        public int X;
        public int Y;

        // 保存 obj 的全部原始 XML 属性（包括 type / x / y）。
        // Type / X / Y 是读取入口；luck / maxChests / 武器等属性暂不解释。
        public Dictionary<string, string> Properties = new();

        public override string ToString()
        {
            return $"{Type} @ ({X}, {Y})";
        }
    }
}
