using Castle.Components.Validator;

namespace MonoRailTemplate.Web.Model
{
    public class FormData
    {
        private string name;
        private string sex;

        [ValidateNonEmpty("Please enter your name.")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ValidateNonEmpty("Please select your sex.")]
        public string Sex
        {
            get { return sex; }
            set { sex = value; }
        }
    }
}
