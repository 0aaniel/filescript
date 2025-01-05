namespace Filescript.Backend.Services
{
    public class ContainerContext
    {
        public string CurrentContainerName { get; private set; }

        public ContainerContext()
        {
            // Default container name or empty
            CurrentContainerName = string.Empty;
        }

        public void SetCurrentContainer(string containerName)
        {
            CurrentContainerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        }
    }
}