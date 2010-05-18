using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using Castle.Facilities.Logging;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Views.NVelocity;
using Castle.MonoRail.WindsorExtension;
using Castle.Windsor;
using MonoRailTemplate.Web.ContainerLogic;

namespace MonoRailTemplate.Web
{
	public class Global : HttpApplication, IContainerAccessor, IMonoRailConfigurationEvents
    {
        private static IWindsorContainer container;

        #region IContainerAccessor Members

        public IWindsorContainer Container
        {
            get { return container; }
        }

        #endregion

		protected void Application_Start(object sender, EventArgs e)
		{
			container = new WindsorContainer();

			container
				.AddFacility("mr", new MonoRailFacility())
				.AddFacility("loggingfacility", new LoggingFacility(LoggerImplementation.Log4net, "Config/logging.config"))
				//.Register(
				//AllTypes.FromAssembly(typeof(Global).Assembly)
				//    .BasedOn(typeof(IController))
				//    .BasedOn(typeof(ViewComponent))
				//    .BasedOn(typeof(IFilter)));

				.Register(
					AllTypes.FromAssembly(typeof(Global).Assembly)
						.BasedOn(typeof(IWebHandle<>))
						.Configure(registration => registration.LifeStyle.Transient))
				.Register(
					AllTypes.FromAssembly(typeof(Global).Assembly)
						.BasedOn(typeof(IWebCommand))
						.Configure(registration => registration.Named(registration.ServiceType.Name).LifeStyle.Transient));

			container.Kernel.RemoveComponent("mr.controllerfactory");
			container.AddComponent("mr.controllerfactory", typeof(IControllerFactory), typeof(ConventionBasedControllerFactory));
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
            container.Dispose();
        }

		public void Configure(IMonoRailConfiguration configuration)
		{
			configuration.UrlConfig.UseExtensions = false;
			configuration.ViewEngineConfig.ViewPathRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views");
			configuration.ViewEngineConfig.ViewEngines.Clear();
			configuration.ViewEngineConfig.ViewEngines.Add(new ViewEngineInfo(typeof (NVelocityViewEngine), false));
		}
    }

	public class ConventionBasedControllerFactory : IControllerFactory
	{
		private readonly IController emptyController;

		public ConventionBasedControllerFactory(IKernel kernel)
		{
			emptyController = new MainController {Kernel = kernel};
		}

		public IController CreateController(string area, string controller)
		{
			return emptyController;
		}

		public IController CreateController(Type controllerType)
		{
			return emptyController;
		}

		public void Release(IController controller)
		{
			
		}
	}

	public class MainController : IController
	{
		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public void Process(IEngineContext engineContext, IControllerContext context)
		{
			string urlRaw = engineContext.UrlInfo.UrlRaw;
			string commandTypeName = urlRaw.Replace("/", String.Empty) + "WebCommand";

			//TODO: Need to write my own ihttphandler becuase the framework is changing the StatusCode to 500
			if (!Kernel.HasComponent(commandTypeName))
			{
				engineContext.Response.StatusCode = 404;
				engineContext.Response.StatusDescription = "Not found";

				if (engineContext.Services.ViewEngineManager.HasTemplate("rescues/404"))
				{
					var parameters = new Dictionary<string, object>();

					engineContext.Services.ViewEngineManager.Process("rescues/404", null, engineContext.Response.Output, parameters);

					return; // gracefully handled
				}

				throw new Exception(String.Format("Command for {0} not found!", commandTypeName));
			}

			context.SelectedViewName = urlRaw;
			var webCommand = Kernel.Resolve<IWebCommand>(commandTypeName);

			MethodInfo genericProcess = typeof(MainController).GetMethod("GenericProcess", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(webCommand.GetType());
			genericProcess.Invoke(this, new object[] { engineContext, context, webCommand });
		}

		private void GenericProcess<T>(IEngineContext engineContext, IControllerContext context, T webCommand) where T : IWebCommand 
		{
			//1 - Authorisation
			//2 - Validation
			//3 - Redirector
			//4 - Handers
			ProcessWebHandles(engineContext, context, webCommand);

			//5 - Base Handlers
			Type baseType = typeof(T);
			while ((baseType = baseType.BaseType) != null)
			{
				if (!typeof(IWebCommand).IsAssignableFrom(baseType))
				{
					break;
				}
				MethodInfo genericProcess = typeof(MainController).GetMethod("ProcessWebHandles", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(baseType);
				genericProcess.Invoke(this, new object[] { engineContext, context, webCommand });
			}

			engineContext.Services.ViewEngineManager.Process(context.SelectedViewName, engineContext.Response.Output, engineContext, this, context);
		}

		private void ProcessWebHandles<T>(IEngineContext engineContext, IControllerContext context, T webCommand) where T : IWebCommand
		{
			var commands = Kernel.ResolveAll<IWebHandle<T>>();
			foreach (var command in commands)
			{
				command.Handle(webCommand);
			}
		}

		public void PreSendView(object view)
		{
		}

		public void PostSendView(object view)
		{
		}

		public event ControllerHandler BeforeAction;
		public event ControllerHandler AfterAction;

		public IKernel Kernel { get; set; }
	}
}