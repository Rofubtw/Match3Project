namespace Match3
{
    public class GridObject<T>
    {
        
        GridSystem2D<GridObject<T>> grid;
        int x;
        int y;
        T candy;

        public GridObject(GridSystem2D<GridObject<T>> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }

        public void SetValue(T candy) => this.candy = candy;

        public T GetValue() => candy;
    }
}