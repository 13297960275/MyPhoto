namespace MyPhotos.Migrations
{
    using Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<MyPhotos.DAL.BaseDBContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(MyPhotos.DAL.BaseDBContext context)
        {
            //This method will be called after migrating to the latest version.

            //You can use the DbSet<T>.AddOrUpdate() helper extension method
            //to avoid creating duplicate seed data.E.g.

            context.PhotoTypes.AddOrUpdate(
                p => p._typeid,
                new PhotoType
                {
                    _typeid = 1,
                    _typename = "�羰",
                    _typedes = "�羰��߸���",
                    _tcover = "AddPhotos.png"
                },
                new PhotoType
                {
                    _typeid = 2,
                    _typename = "��Ů",
                    _typedes = "����Ů",
                    _tcover = "AddPhotos.png"
                },
                new PhotoType
                {
                    _typeid = 3,
                    _typename = "����",
                    _typedes = "������С����",
                    _tcover = "AddPhotos.png"
                }
            );
            context.Roles.AddOrUpdate(
                r => r.RoleID,
                new Role
                {
                    RoleID = 1,
                    RoleName = "ע���û�",
                    RoleDescription = "��ͨȨ��"
                },
                new Role
                {
                    RoleID = 2,
                    RoleName = "����Ա",
                    RoleDescription = "����ԱȨ��"
                },
                new Role
                {
                    RoleID = 3,
                    RoleName = "��������Ա",
                    RoleDescription = "���Ȩ��"
                }
            );
        }
    }
}
