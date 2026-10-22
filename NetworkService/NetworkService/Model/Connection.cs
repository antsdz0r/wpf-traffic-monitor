namespace NetworkService.Model
{
    
    public class Connection
    {
        public Entity First { get; }
        public Entity Second { get; }

        public Connection(Entity first, Entity second)
        {
            First = first;
            Second = second;
        }

        public bool Involves(Entity e)
        {
            return First == e || Second == e;
        }

        public bool IsSamePair(Entity a, Entity b)
        {
            return (First == a && Second == b) || (First == b && Second == a);
        }
    }
}
