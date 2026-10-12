using LF.Application.Common.Documents;

namespace LF.Application.Common.Interfaces;

public interface ICoursePdfRenderer
{
    byte[] Render(CourseDocument document);
}
