using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace NoryMusicLMS_VS.Pages
{
    [Authorize(Roles = "Admin")]
    public class SampleDataModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SampleDataModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            
            string[] roleNames = { "Admin", "Instructor", "Student" };
            foreach (var roleName in roleNames)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            
            var instructorEmail = "instructor@norymusic.com";
            var instructorUser = await _userManager.FindByEmailAsync(instructorEmail);
            if (instructorUser == null)
            {
                instructorUser = new ApplicationUser
                {
                    UserName = instructorEmail,
                    Email = instructorEmail,
                    FirstName = "Maria",
                    LastName = "Johnson",
                    DateOfBirth = new DateTime(1985, 5, 15),
                    Bio = "Professional pianist and music theory instructor with 15 years of teaching experience.",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(instructorUser, "Instructor@123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(instructorUser, "Instructor");
                }
            }

            var studentEmail = "student@norymusic.com";
            var studentUser = await _userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FirstName = "Alex",
                    LastName = "Chen",
                    DateOfBirth = new DateTime(2000, 8, 22),
                    Bio = "Music enthusiast looking to learn music theory and improve ear training skills.",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(studentUser, "Student@123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(studentUser, "Student");
                }
            }

            
            var musicTheory101 = await _context.Courses.FirstOrDefaultAsync(c => c.Title == "Music Theory Fundamentals");
            if (musicTheory101 == null && instructorUser != null)
            {
                musicTheory101 = new Course
                {
                    Title = "Music Theory Fundamentals",
                    Description = "Learn the basics of music theory including notes, scales, chords, and rhythm. Perfect for beginners!",
                    InstructorId = instructorUser.Id,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(3),
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1511671782739-c97d3f57b4ba?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwyNzQ2Nzl8MHwxfHNlYXJjaHwxfHxtdXNpYyUyMHRoZW9yeXxlbnwwfHx8fDE2NTkzODc4MDg&ixlib=rb-1.2.1&q=80&w=400"
                };

                _context.Courses.Add(musicTheory101);
                await _context.SaveChangesAsync();
            }

            var chordProgressions = await _context.Courses.FirstOrDefaultAsync(c => c.Title == "Chord Progressions & Harmony");
            if (chordProgressions == null && instructorUser != null)
            {
                chordProgressions = new Course
                {
                    Title = "Chord Progressions & Harmony",
                    Description = "Master common chord progressions used in popular music and learn how to create your own harmonic sequences.",
                    InstructorId = instructorUser.Id,
                    StartDate = DateTime.Today.AddMonths(1),
                    EndDate = DateTime.Today.AddMonths(4),
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1578662996442-48f60103fc96?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwyNjQyMzh8MHwxfHNlYXJjaHwzfHxjaG9yZCUyMHByb2dyZXNzaW9ufGVufDB8fHx8MTY1OTM4NzgwNw&ixlib=rb-1.2.1&q=80&w=400"
                };

                _context.Courses.Add(chordProgressions);
                await _context.SaveChangesAsync();
            }

            var earTraining = await _context.Courses.FirstOrDefaultAsync(c => c.Title == "Ear Training Essentials");
            if (earTraining == null && instructorUser != null)
            {
                earTraining = new Course
                {
                    Title = "Ear Training Essentials",
                    Description = "Develop your ability to identify notes, intervals, chords, and rhythms by ear. Essential for all musicians.",
                    InstructorId = instructorUser.Id,
                    StartDate = DateTime.Today.AddMonths(2),
                    EndDate = DateTime.Today.AddMonths(5),
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1581091020534-68fa87c9d15e?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwyNjQyMzh8MHwxfHNlYXJjaHw0fHxlYXIlMjByYWluaW5nfGVufDB8fHx8MTY1OTM4NzgwOA&ixlib=rb-1.2.1&q=80&w=400"
                };

                _context.Courses.Add(earTraining);
                await _context.SaveChangesAsync();
            }

            
            if (musicTheory101 != null)
            {
                var lesson1 = await _context.Lessons.FirstOrDefaultAsync(l => l.Title == "Introduction to Musical Notes" && l.CourseId == musicTheory101.Id);
                if (lesson1 == null)
                {
                    lesson1 = new Lesson
                    {
                        Title = "Introduction to Musical Notes",
                        Content = "<p>Welcome to your first music theory lesson! In this lesson, we'll learn about the basic building blocks of music: musical notes.</p><p>In Western music, we use a system of 12 notes that repeat in patterns called octaves. These notes are named using the first seven letters of the alphabet: A, B, C, D, E, F, and G.</p><p>After G, the pattern repeats starting again at A, but in a higher octave. The distance between two notes with the same name is called an octave.</p><p>Notes can be natural (no sharps or flats), sharp (raised by a half step), or flat (lowered by a half step). The black keys on a piano represent the sharps and flats.</p>",
                        CourseId = musicTheory101.Id,
                        OrderIndex = 1,
                        VideoUrl = "https://www.youtube.com/embed/k9uRk07nY8w",
                        AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3",
                        NotationUrl = "https://www.musicnotes.com/images/productimages/large/mng/SM-000002255.gif"
                    };

                    _context.Lessons.Add(lesson1);
                    await _context.SaveChangesAsync();
                }

                var lesson2 = await _context.Lessons.FirstOrDefaultAsync(l => l.Title == "Understanding Scales" && l.CourseId == musicTheory101.Id);
                if (lesson2 == null)
                {
                    lesson2 = new Lesson
                    {
                        Title = "Understanding Scales",
                        Content = "<p>A scale is a sequence of musical notes ordered by pitch. The most common scales in Western music are major and minor scales.</p><p>The major scale follows a specific pattern of whole steps (W) and half steps (H): W-W-H-W-W-W-H. For example, the C major scale is: C-D-E-F-G-A-B-C.</p><p>Minor scales have a different pattern. The natural minor scale follows: W-H-W-W-H-W-W. For example, the A minor scale is: A-B-C-D-E-F-G-A.</p><p>Scales are important because they form the foundation for melody and harmony in music.</p>",
                        CourseId = musicTheory101.Id,
                        OrderIndex = 2,
                        VideoUrl = "https://www.youtube.com/embed/4767j-kLsSE",
                        AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3",
                        NotationUrl = "https://www.musicnotes.com/images/productimages/large/mng/SM-000002256.gif"
                    };

                    _context.Lessons.Add(lesson2);
                    await _context.SaveChangesAsync();
                }

                var lesson3 = await _context.Lessons.FirstOrDefaultAsync(l => l.Title == "Building Chords" && l.CourseId == musicTheory101.Id);
                if (lesson3 == null)
                {
                    lesson3 = new Lesson
                    {
                        Title = "Building Chords",
                        Content = "<p>A chord is a group of three or more notes played together. The most basic chord is a triad, which consists of three notes: the root, the third, and the fifth.</p><p>To build a triad, start with a root note, then skip one note to get the third, and skip another note to get the fifth.</p><p>In the C major scale (C-D-E-F-G-A-B-C):<br/>- C major triad: C-E-G (root C, skip D to get E, skip F to get G)<br/>- D minor triad: D-F-A (root D, skip E to get F, skip G to get A)<br/>- E minor triad: E-G-B (root E, skip F to get G, skip A to get B)</p><p>Chords can be major, minor, diminished, or augmented depending on the intervals between the notes.</p>",
                        CourseId = musicTheory101.Id,
                        OrderIndex = 3,
                        VideoUrl = "https://www.youtube.com/embed/G9dal3tT-_c",
                        AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3",
                        NotationUrl = "https://www.musicnotes.com/images/productimages/large/mng/SM-000002257.gif"
                    };

                    _context.Lessons.Add(lesson3);
                    await _context.SaveChangesAsync();
                }
            }

            
            if (chordProgressions != null)
            {
                var lesson1 = await _context.Lessons.FirstOrDefaultAsync(l => l.Title == "Introduction to Chord Progressions" && l.CourseId == chordProgressions.Id);
                if (lesson1 == null)
                {
                    lesson1 = new Lesson
                    {
                        Title = "Introduction to Chord Progressions",
                        Content = "<p>A chord progression is a sequence of chords played in a particular order. Chord progressions form the harmonic foundation of a piece of music.</p><p>Some chord progressions are used so frequently that they've become standard in popular music. Learning these common progressions will help you play thousands of songs!</p><p>The most common chord progression in pop music is I-V-vi-IV (one-five-six-four). In the key of C major, this would be: C-G-Am-F.</p><p>Other common progressions include:<br/>- I-IV-V-I (one-four-five-one): C-F-G-C<br/>- ii-V-I (two-five-one): Dm-G-C<br/>- I-vi-IV-V (one-six-four-five): C-Am-F-G</p>",
                        CourseId = chordProgressions.Id,
                        OrderIndex = 1,
                        VideoUrl = "https://www.youtube.com/embed/Kmhm7eehKX4",
                        AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-4.mp3",
                        NotationUrl = "https://www.musicnotes.com/images/productimages/large/mng/SM-000002258.gif"
                    };

                    _context.Lessons.Add(lesson1);
                    await _context.SaveChangesAsync();
                }

                var lesson2 = await _context.Lessons.FirstOrDefaultAsync(l => l.Title == "Analyzing Popular Songs" && l.CourseId == chordProgressions.Id);
                if (lesson2 == null)
                {
                    lesson2 = new Lesson
                    {
                        Title = "Analyzing Popular Songs",
                        Content = "<p>Let's look at some real-world examples of chord progressions in popular songs:</p><p><strong>Let It Be by The Beatles:</strong> C-G-Am-F (I-V-vi-IV) - This progression repeats throughout most of the song.</p><p><strong>Someone Like You by Adele:</strong> A-E-F#m-D (I-V-vi-IV in key of A)</p><p><strong>No Woman No Cry by Bob Marley:</strong> C-G-Am-F (I-V-vi-IV)</p><p><strong>With or Without You by U2:</strong> D-A-Bm-G (I-V-vi-IV in key of D)</p><p>Notice how many popular songs use the same I-V-vi-IV progression? This is why learning common progressions is so valuable!</p>",
                        CourseId = chordProgressions.Id,
                        OrderIndex = 2,
                        VideoUrl = "https://www.youtube.com/embed/0b4Y-WBsqfs",
                        AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-5.mp3",
                        NotationUrl = "https://www.musicnotes.com/images/productimages/large/mng/SM-000002259.gif"
                    };

                    _context.Lessons.Add(lesson2);
                    await _context.SaveChangesAsync();
                }
            }

            
            if (earTraining != null)
            {
                var lesson1 = await _context.Lessons.FirstOrDefaultAsync(l => l.Title == "Introduction to Ear Training" && l.CourseId == earTraining.Id);
                if (lesson1 == null)
                {
                    lesson1 = new Lesson
                    {
                        Title = "Introduction to Ear Training",
                        Content = "<p>Ear training is the process of developing your ability to identify musical elements purely by hearing them. This includes:</p><ul><li>Identifying individual notes</li><li>Recognizing intervals (the distance between two notes)</li><li>Identifying chords and chord progressions</li><li>Recognizing rhythms and time signatures</li></ul><p>Like any skill, ear training improves with consistent practice. Even just 10-15 minutes a day can lead to significant improvement over time.</p><p>In this course, we'll start with the basics: identifying individual notes, then move on to intervals, chords, and finally progressions and rhythms.</p>",
                        CourseId = earTraining.Id,
                        OrderIndex = 1,
                        VideoUrl = "https://www.youtube.com/embed/w9wOhYvbx-s",
                        AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-6.mp3",
                        NotationUrl = "https://www.musicnotes.com/images/productimages/large/mng/SM-000002260.gif"
                    };

                    _context.Lessons.Add(lesson1);
                    await _context.SaveChangesAsync();
                }

                var lesson2 = await _context.Lessons.FirstOrDefaultAsync(l => l.Title == "Identifying Notes by Ear" && l.CourseId == earTraining.Id);
                if (lesson2 == null)
                {
                    lesson2 = new Lesson
                    {
                        Title = "Identifying Notes by Ear",
                        Content = "<p>The first step in ear training is learning to identify individual notes by ear. This skill is called 'pitch recognition' or 'absolute pitch' when you can identify a note without any reference.</p><p>Most people develop 'relative pitch' instead, which means you can identify notes relative to a reference note you've just heard.</p><p>To practice note identification:</p><ol><li>Listen to a reference note (like middle C)</li><li>Listen to a mystery note</li><li>Determine if the mystery note is higher or lower than the reference</li><li>Guess how many steps away it is</li><li>Name the mystery note</li></ol><p>Start with notes that are close together (within an octave) and gradually increase the distance as you improve.</p>",
                        CourseId = earTraining.Id,
                        OrderIndex = 2,
                        VideoUrl = "https://www.youtube.com/embed/3JZ_D3ELwOQ",
                        AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-7.mp3",
                        NotationUrl = "https://www.musicnotes.com/images/productimages/large/mng/SM-000002261.gif"
                    };

                    _context.Lessons.Add(lesson2);
                    await _context.SaveChangesAsync();
                }
            }

            
            if (musicTheory101 != null)
            {
                
                var firstLesson = await _context.Lessons.FirstOrDefaultAsync(l => l.CourseId == musicTheory101.Id);
                if (firstLesson != null)
                {
                    
                    var quiz1 = await _context.Quizzes.FirstOrDefaultAsync(q => q.Title == "Notes Quiz" && q.LessonId == firstLesson.Id);
                    if (quiz1 == null)
                    {
                        quiz1 = new Quiz
                        {
                            Title = "Notes Quiz",
                            Description = "Test your knowledge of musical notes",
                            Type = QuizType.Theory,
                            LessonId = firstLesson.Id,
                            IsActive = true
                        };

                        _context.Quizzes.Add(quiz1);
                        await _context.SaveChangesAsync();
                    }

                    
                    if (quiz1 != null)
                    {
                        var question1 = await _context.QuizQuestions.FirstOrDefaultAsync(q => q.QuizId == quiz1.Id && q.QuestionText == "What are the seven natural notes in music?");
                        if (question1 == null)
                        {
                            question1 = new QuizQuestion
                            {
                                QuizId = quiz1.Id,
                                QuestionText = "What are the seven natural notes in music?",
                                Type = QuestionType.FillInBlank,
                                Points = 10,
                                OrderIndex = 1,
                                CorrectAnswerText = "A B C D E F G"
                            };

                            _context.QuizQuestions.Add(question1);
                            await _context.SaveChangesAsync();
                        }

                        var question2 = await _context.QuizQuestions.FirstOrDefaultAsync(q => q.QuizId == quiz1.Id && q.QuestionText == "Which note comes after G in the musical alphabet?");
                        if (question2 == null)
                        {
                            question2 = new QuizQuestion
                            {
                                QuizId = quiz1.Id,
                                QuestionText = "Which note comes after G in the musical alphabet?",
                                Type = QuestionType.MultipleChoice,
                                Points = 10,
                                OrderIndex = 2,
                                OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]",
                                CorrectAnswerIndex = 1
                            };

                            _context.QuizQuestions.Add(question2);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            
            if (earTraining != null)
            {
                var firstLesson = await _context.Lessons.FirstOrDefaultAsync(l => l.CourseId == earTraining.Id);
                if (firstLesson != null)
                {
                    var earQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Title == "Note Identification Quiz" && q.LessonId == firstLesson.Id);
                    if (earQuiz == null)
                    {
                        earQuiz = new Quiz
                        {
                            Title = "Note Identification Quiz",
                            Description = "Test your ability to identify notes by ear",
                            Type = QuizType.EarTraining,
                            LessonId = firstLesson.Id,
                            IsActive = true
                        };

                        _context.Quizzes.Add(earQuiz);
                        await _context.SaveChangesAsync();
                    }

                    
                    if (earQuiz != null)
                    {
                        var audioQuestion = await _context.QuizQuestions.FirstOrDefaultAsync(q => q.QuizId == earQuiz.Id && q.QuestionText == "Listen to the audio and identify the note being played");
                        if (audioQuestion == null)
                        {
                            audioQuestion = new QuizQuestion
                            {
                                QuizId = earQuiz.Id,
                                QuestionText = "Listen to the audio and identify the note being played",
                                Type = QuestionType.AudioIdentification,
                                Points = 20,
                                OrderIndex = 1,
                                AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-8.mp3",
                                CorrectAnswerText = "C"
                            };

                            _context.QuizQuestions.Add(audioQuestion);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            TempData["SuccessMessage"] = "Sample data generated successfully! You can now log in with:\n\nAdmin: admin@norymusic.com / Admin@123\nInstructor: instructor@norymusic.com / Instructor@123\nStudent: student@norymusic.com / Student@123";

            return RedirectToPage("/Index");
        }
    }
}