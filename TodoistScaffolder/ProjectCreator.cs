using System;
using System.Collections.Generic;
using System.Text;

namespace TodoistScaffolder
{
    public class ProjectCreator
    {
        public string CreateJsonFor300Projects()
        {
            const int noProjects = 300;
            var stringBuilder = new StringBuilder("[");
            for (var i = 1; i <= noProjects; i++)
            {
                var project = $"{{\"type\": \"project_add\",\"temp_id\": \"Pre78961a3910d4f1489caf38e11e9c6de\",\"args\": {{\"name\": \"{i}\",\"item_order\": {i},\"indent\": {new Random().Next(1, 3)},\"color\": 42}},\"uuid\": \"\"}}";
                stringBuilder.Append(project);
                if (i < noProjects)
                    stringBuilder.Append(",");
            }
            return stringBuilder.ToString();
        } 
    }
}
