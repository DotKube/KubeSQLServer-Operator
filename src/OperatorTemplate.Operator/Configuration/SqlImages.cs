

namespace SqlServerOperator.Configuration;

public class SqlServerImages
{

    public string BaseImage = "ghcr.io/dotkube/sql-server";

    public string UbuntuBasedImage(int sqlServerEdition = 2022, bool includeFullTextSearch = false)
    {
        string sqlServerYearEditionFormattedForImage = sqlServerEdition == 2019 ? "19" : "22";

        return includeFullTextSearch ?
            $"{BaseImage}:{sqlServerYearEditionFormattedForImage}-ubuntu-fts" :
            $"{BaseImage}:{sqlServerYearEditionFormattedForImage}-ubuntu";
    }

    public string UbuntuBasedImage(string sqlServerEdition = "2022", bool includeFullTextSearch = false)
    {
        string sqlServerYearEditionFormattedForImage = sqlServerEdition == "2019" ? "19" : "22";

        return includeFullTextSearch ?
            $"{BaseImage}:{sqlServerYearEditionFormattedForImage}-ubuntu-fts" :
            $"{BaseImage}:{sqlServerYearEditionFormattedForImage}-ubuntu";
    }
}