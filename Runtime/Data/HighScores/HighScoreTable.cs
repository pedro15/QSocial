using System.Collections.Generic;

namespace QSocial.Data.HighScores
{
    [System.Serializable]
    public sealed class HighScoreTable
    {
        public string TableName;
        public HighScoreItem[] Items;

        public HighScoreTable() { }

        public HighScoreTable(string TableName,  List<HighScoreItem> Items )
        {
            this.TableName = TableName;
            this.Items = Items.ToArray();
        }
    }
}