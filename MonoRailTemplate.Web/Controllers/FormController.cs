using Castle.MonoRail.Framework;
using MonoRailTemplate.Web.Model;

namespace MonoRailTemplate.Web.Controllers
{
	[Layout("default")]
	[Rescue("error")]
	public class FormController : SmartDispatcherController
	{
		public void Index()
		{
			PropertyBag["userName"] = Context.CurrentUser.Identity.Name;
		}


		public void Main()
		{
			//PropertyBag["formDatatype"] = typeof (FormData);
			PropertyBag["sexTypes"] = new[] { "Male", "Female" };
			PropertyBag["formData"] = Flash["formData"] ?? new FormData();
		}

		[AccessibleThrough(Verb.Post)]
		public void MainSave([DataBind("formData", Validate = true)] FormData formData)
		{
			Flash["formData"] = formData;

			if (HasValidationError(formData))
			{
				Flash["errors"] = GetErrorSummary(formData);
				RedirectToReferrer();

				return;
			}

			RedirectToAction("Confirm");
		}

		public void Confirm()
		{
			PropertyBag["formData"] = Flash["formData"];
		}
	}
}
