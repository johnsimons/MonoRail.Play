using System.Diagnostics;
using MonoRailTemplate.Web.ContainerLogic;

namespace MonoRailTemplate.Web.Handlers
{
	public class ProductsListHandler : IWebHandle<ProductsListWebCommand>
	{
		public void Handle(ProductsListWebCommand command)
		{
			Debug.WriteLine("Executing ProductsListWebCommand");
		}
	}

	public class ProductsHandler : IWebHandle<ProductsWebCommand>
	{
		public void Handle(ProductsWebCommand command)
		{
			Debug.WriteLine("Executing ProductsWebCommand");
		}
	}

	public class WebCommandHandler : IWebHandle<WebCommand>
	{
		public void Handle(WebCommand command)
		{
			Debug.WriteLine("Executing WebCommand");
		}
	}
}