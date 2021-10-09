using System.Collections.Generic;

namespace BatToshoRESTApp.Components
{
    public class ListSearch
    {
        public ListSearch(List<SearchResultTemplate> array)
        {
            Array = array;
        }

        public List<SearchResultTemplate> Array { get; }
    }
}