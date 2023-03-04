namespace Rwp
{
    public class Notices
    {
        public static string ConvertSqlStringToHtml(string sql)
        {
            // this could be done way faster using stringBuilder and parsing the string once...

            sql = sql.Replace("~B", "<b>").Replace("~b", "</b>");
            sql = sql.Replace("~I", "<i>").Replace("~i", "</i>");
            sql = sql.Replace("~U", "<ul>").Replace("~ul", "</ul>");
            sql = sql.Replace("~N", "<br>");

            return sql;

        }
    }
}
