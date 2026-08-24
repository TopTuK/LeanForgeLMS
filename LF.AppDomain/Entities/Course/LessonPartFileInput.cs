using LF.AppDomain.Entities.Storage;

namespace LF.AppDomain.Entities.Course;

public sealed record LessonPartFileInput(string FileName, StorageObject StorageObject);
