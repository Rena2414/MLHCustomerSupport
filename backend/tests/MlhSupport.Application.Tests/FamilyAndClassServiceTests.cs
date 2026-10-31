using MlhSupport.Application.Classes;
using MlhSupport.Application.Common;
using MlhSupport.Application.Families;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Tests;

public class FamilyAndClassServiceTests
{
    private static (FamilyService Families, ClassService Classes, InMemoryStore Store) Build()
    {
        var store = new InMemoryStore();
        var classes = new ClassService(store, store, store);
        return (new FamilyService(store, store, classes, store), classes, store);
    }

    [Fact]
    public async Task Parent_LinksChildByLoginAndSeesTheirClasses()
    {
        var (families, _, store) = Build();
        var parent = store.AddUser("anna", UserRole.Parent);
        var child = store.AddUser("emma", UserRole.Student);
        store.AddClass(store.AddUser("harper", UserRole.Teacher), child);

        await families.LinkChildAsync(parent.Id, new LinkChildRequest("EMMA"), CancellationToken.None);

        var children = await families.GetChildrenAsync(parent.Id, CancellationToken.None);
        Assert.Equal(child.Id, Assert.Single(children).Child.Id);
        Assert.Equal("HARPER", Assert.Single(children[0].Classes).TeacherName);
    }

    [Fact]
    public async Task Parent_CannotLinkTeacherOrSameChildTwice()
    {
        var (families, _, store) = Build();
        var parent = store.AddUser("anna", UserRole.Parent);
        store.AddUser("harper", UserRole.Teacher);
        store.AddUser("emma", UserRole.Student);

        await Assert.ThrowsAsync<NotFoundException>(
            () => families.LinkChildAsync(parent.Id, new LinkChildRequest("harper"), CancellationToken.None));
        await families.LinkChildAsync(parent.Id, new LinkChildRequest("emma"), CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(
            () => families.LinkChildAsync(parent.Id, new LinkChildRequest("emma"), CancellationToken.None));
    }

    [Fact]
    public async Task Student_CannotManageChildren()
    {
        var (families, _, store) = Build();
        var student = store.AddUser("emma", UserRole.Student);

        await Assert.ThrowsAsync<ForbiddenException>(() => families.GetChildrenAsync(student.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Teacher_CreatesClassAndEnrollsStudents()
    {
        var (_, classes, store) = Build();
        var teacher = store.AddUser("harper", UserRole.Teacher);
        store.AddUser("emma", UserRole.Student);

        var created = await classes.CreateAsync(teacher.Id, new CreateClassRequest("Math 5A", "Mathematics"), CancellationToken.None);
        var updated = await classes.EnrollAsync(teacher.Id, created.Id, new EnrollStudentRequest("emma"), CancellationToken.None);

        Assert.Equal("emma", Assert.Single(updated.Students).Login);
        await Assert.ThrowsAsync<ConflictException>(
            () => classes.EnrollAsync(teacher.Id, created.Id, new EnrollStudentRequest("emma"), CancellationToken.None));
    }

    [Fact]
    public async Task Teacher_CannotChangeAnotherTeachersClass()
    {
        var (_, classes, store) = Build();
        var owner = store.AddUser("harper", UserRole.Teacher);
        var intruder = store.AddUser("lee", UserRole.Teacher);
        var student = store.AddUser("emma", UserRole.Student);
        var schoolClass = store.AddClass(owner, student);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => classes.RemoveAsync(intruder.Id, schoolClass.Id, student.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(
            () => classes.CreateAsync(student.Id, new CreateClassRequest("X", "Y"), CancellationToken.None));
    }

    [Fact]
    public async Task VisibleClasses_DependOnRole()
    {
        var (_, classes, store) = Build();
        var admin = store.AddUser("admin", UserRole.Admin);
        var harper = store.AddUser("harper", UserRole.Teacher);
        var lee = store.AddUser("lee", UserRole.Teacher);
        var emma = store.AddUser("emma", UserRole.Student);
        store.AddClass(harper, emma);
        store.AddClass(lee);

        Assert.Equal(2, (await classes.GetVisibleAsync(admin.Id, CancellationToken.None)).Count);
        Assert.Single(await classes.GetVisibleAsync(lee.Id, CancellationToken.None));
        Assert.Equal(harper.Id, Assert.Single(await classes.GetVisibleAsync(emma.Id, CancellationToken.None)).TeacherId);
    }
}
