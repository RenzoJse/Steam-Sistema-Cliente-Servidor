namespace Comunicacion.Dominio;

public class ReviewManager
{
    private static List<Review> Reviews = new List<Review>();
    private static object _lock = new object();

    public ReviewManager()
    {
            
    }
        
    public List<Review> GetAllReviews()
    {
        lock (_lock)
        {
            return Reviews;
        }
    }
    
    public void AddReview(Review review)
    {
        lock (_lock)
        {
            Reviews.Add(review);
        }
    }
    
}