# hashtag_instagramistsoeinscheissdreck

## How to use?
1. Pass a SQLite connection string to ``new SqliteConnection("Data Source=/home/daniel/Downloads/unterdb_DE.db;Cache=Shared")``` or adapt the connection to a different database.
2. Adapt the query ```select id, tags from locations```to match your database. Use aliases for 'id' and 'tags' if required.
3. Adapt the values for the variables 'centerTag', 'minTagOccurrence' and 'minCoOccurrence' if required.
4. Replace the output path for the nodes and edges CSV ('/home/daniel/Desktop/tag_edges.csv' and '/home/daniel/Desktop/tag_nodes.csv' in the code)
5. Run and have fun
