namespace NoryMusicLMS_VS.Services;

using NoryMusicLMS_VS.Models;
public static class MediaFallbacks
{
    private static readonly string[] CourseImages =
    {
        "/images/course-music-theory.svg",
        "/images/course-beginner-guitar.svg",
        "/images/course-rhythm-reading.svg",
        "/images/course-chords-songwriting.svg"
    };

    private static readonly string[] LessonImages =
    {
        "/images/lesson-staff.svg",
        "/images/lesson-notes.svg",
        "/images/lesson-guitar.svg",
        "/images/lesson-chords.svg",
        "/images/lesson-rhythm.svg"
    };

    private static readonly string[] QuizImages =
    {
        "/images/quiz-note-values.svg",
        "/images/quiz-note-names.svg",
        "/images/quiz-rhythm.svg",
        "/images/quiz-chords.svg"
    };

    public static string CourseImage(Course course)
    {
        if (!string.IsNullOrWhiteSpace(course.ImageUrl))
            return course.ImageUrl;
        return CourseImages[Math.Abs(course.Id) % CourseImages.Length];
    }

    public static string LessonImage(Lesson lesson)
    {
        if (!string.IsNullOrWhiteSpace(lesson.ImageUrl))
            return lesson.ImageUrl;
        return LessonImages[Math.Abs(lesson.Id) % LessonImages.Length];
    }

    public static string QuizImage(Quiz quiz)
    {
        if (!string.IsNullOrWhiteSpace(quiz.ImageUrl))
            return quiz.ImageUrl;
        return QuizImages[Math.Abs(quiz.Id) % QuizImages.Length];
    }
}
