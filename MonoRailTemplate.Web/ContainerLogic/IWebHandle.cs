namespace MonoRailTemplate.Web.ContainerLogic
{
    public interface IWebHandle<T> where T: IWebCommand
    {
		void Handle(T command);
    }
}