namespace NLogDemo.Worker
{
    public class MessageProcessor
    {
        readonly MyLogger logger = new MyLogger();

        public void DoWork()
        {
            // TODO: Add implementation logic
            
            logger.Info("doing some worky worky stuff");
        }
    }
}