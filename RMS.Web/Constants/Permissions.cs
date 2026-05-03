namespace RMS.Web.Constants
{
    public static class Permissions
    {
        public static class Orders
        {
            public const string View = "Permissions.Orders.View";
            public const string Create = "Permissions.Orders.Create";
            public const string Edit = "Permissions.Orders.Edit";
            public const string Cancel = "Permissions.Orders.Cancel";
            public const string UpdateStatus = "Permissions.Orders.UpdateStatus";
            public const string AssignDelivery = "Permissions.Orders.AssignDelivery";
        }

        public static class Menu
        {
            public const string View = "Permissions.Menu.View";
            public const string Manage = "Permissions.Menu.Manage";
        }

        public static class Branches
        {
            public const string View = "Permissions.Branches.View";
            public const string Manage = "Permissions.Branches.Manage";
        }

        public static class Users
        {
            public const string View = "Permissions.Users.View";
            public const string Manage = "Permissions.Users.Manage";
        }

        public static class Reports
        {
            public const string View = "Permissions.Reports.View";
            public const string Export = "Permissions.Reports.Export";
        }

        public static class Customers
        {
            public const string View = "Permissions.Customers.View";
            public const string Create = "Permissions.Customers.Create";
            public const string Edit = "Permissions.Customers.Edit";
            public const string Delete = "Permissions.Customers.Delete";
        }

        public static class System
        {
            public const string ViewAuditLogs = "Permissions.System.ViewAuditLogs";
            public const string ManageSettings = "Permissions.System.ManageSettings";
        }

        public static List<string> GetAllPermissions()
        {
            return new List<string>
            {
                Orders.View, Orders.Create, Orders.Edit, Orders.Cancel, Orders.UpdateStatus, Orders.AssignDelivery,
                Menu.View, Menu.Manage,
                Branches.View, Branches.Manage,
                Users.View, Users.Manage,
                Reports.View, Reports.Export,
                Customers.View, Customers.Create, Customers.Edit, Customers.Delete,
                System.ViewAuditLogs, System.ManageSettings
            };
        }
    }
}
