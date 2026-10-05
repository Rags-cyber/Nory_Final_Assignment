import re, sys, os

# Read the file as binary, detect encoding from BOM
with open('database.sql', 'rb') as f:
    raw = f.read()

if raw[:2] == b'\xff\xfe':
    encoding = 'utf-16-le'
    sql = raw.decode('utf-16-le')
    print(f"Detected UTF-16LE, decoded {len(raw)} bytes to {len(sql)} chars")
elif raw[:3] == b'\xef\xbb\xbf':
    encoding = 'utf-8-sig'
    sql = raw.decode('utf-8-sig')
    print(f"Detected UTF-8-SIG, decoded {len(raw)} bytes to {len(sql)} chars")
else:
    encoding = 'utf-8'
    sql = raw.decode('utf-8', errors='replace')
    print(f"No BOM detected, decoded as UTF-8: {len(sql)} chars")

# Normalize line endings
sql = sql.replace('\r\n', '\n').replace('\r', '\n')

seed_tables = {'AspNetRoles','AspNetUserRoles','AspNetUsers','ChordSongs',
               'Courses','Enrollments','LessonCompletions','Lessons',
               'QuizAnswers','QuizAttempts','QuizQuestions','Quizzes','StudentAwards'}

def get_table(line):
    m = re.search(r'\[dbo\]\.\[([^\]]+)\]', line)
    return m.group(1) if m else None

lines = sql.split('\n')
out = []
i = 0
ins_cnt = 0
con_cnt = 0
id_block_cnt = 0

# Pre-scan SET IDENTITY_INSERT blocks
id_blocks = {}
j = 0
while j < len(lines):
    if (lines[j].strip().startswith('SET IDENTITY_INSERT') and 
        'ON' in lines[j] and 'OFF' not in lines[j]):
        tbl = get_table(lines[j])
        on_idx = j
        k = j + 1
        while k < len(lines):
            if (lines[k].strip().startswith('SET IDENTITY_INSERT') and 
                'OFF' in lines[k]):
                off_idx = k
                id_blocks[on_idx] = (on_idx, off_idx, tbl)
                j = k + 1
                break
            k += 1
        else:
            j += 1
    else:
        j += 1

while i < len(lines):
    ls = lines[i].strip()
    t = get_table(lines[i])

    # SET IDENTITY_INSERT lines - pass through
    if ls.startswith('SET IDENTITY_INSERT'):
        out.append(lines[i])
        i += 1
        continue

    # === SEED INSERT ===
    if ls.startswith('INSERT [dbo].[') and t in seed_tables:
        # Check if inside an IDENTITY block
        in_id = False
        id_on = id_off = None
        for on_idx, (on, off, tbl) in id_blocks.items():
            if on_idx < i < off:
                in_id = True
                id_on = on
                id_off = off
                break

        if in_id:
            kk = i
            while kk < id_off:
                if lines[kk].strip().startswith('INSERT [dbo].[') and lines[kk].strip() != 'GO':
                    it = get_table(lines[kk])
                    mm = re.search(r'VALUES\s*\(\s*([^,\)]+)', lines[kk])
                    if mm:
                        pv = mm.group(1).strip()
                        if pv.startswith("N'") or pv.startswith("'"):
                            inner = pv.strip("N'").strip("'").replace("'", "''")
                            chk = "IF NOT EXISTS (SELECT 1 FROM [dbo].[" + it + "] WHERE [Id] = N'" + inner + "')"
                        elif pv.lstrip('-').isdigit():
                            chk = "IF NOT EXISTS (SELECT 1 FROM [dbo].[" + it + "] WHERE [Id] = " + pv + ")"
                        else:
                            chk = "IF NOT EXISTS (SELECT 1 FROM [dbo].[" + it + "] WHERE [Id] = " + pv + ")"
                        ib = [lines[kk]]
                        nn = kk + 1
                        while nn < id_off:
                            ns = lines[nn].strip()
                            if ns.startswith('INSERT [dbo].[') or ns.startswith('SET IDENTITY_INSERT') or ns == 'GO':
                                break
                            ib.append(lines[nn])
                            nn += 1
                        out.append(chk + "\nBEGIN\n" + "\n".join(ib) + "\nEND")
                        ins_cnt += 1
                        kk = nn
                kk += 1

            out.append(lines[id_off])
            if id_off + 1 < len(lines) and lines[id_off + 1].strip() == 'GO':
                out.append(lines[id_off + 1])
                i = id_off + 2
            else:
                i = id_off + 1
            id_block_cnt += 1
            continue

        end_j = i + 1
        while end_j < len(lines):
            es = lines[end_j].strip()
            if es == 'GO':
                end_j += 1
                break
            if es.startswith('INSERT [dbo].[') or es.startswith('SET IDENTITY_INSERT'):
                break
            if end_j >= len(lines) - 1:
                end_j += 1
                break
            end_j += 1

        il = lines[i]
        it = get_table(il)
        mm = re.search(r'VALUES\s*\(\s*([^,\)]+)', il)
        if mm:
            pv = mm.group(1).strip()
            if pv.startswith("N'") or pv.startswith("'"):
                inner = pv.strip("N'").strip("'").replace("'", "''")
                chk = "IF NOT EXISTS (SELECT 1 FROM [dbo].[" + it + "] WHERE [Id] = N'" + inner + "')"
            elif pv.lstrip('-').isdigit():
                chk = "IF NOT EXISTS (SELECT 1 FROM [dbo].[" + it + "] WHERE [Id] = " + pv + ")"
            else:
                chk = "IF NOT EXISTS (SELECT 1 FROM [dbo].[" + it + "] WHERE [Id] = " + pv + ")"
            body = '\n'.join(lines[i:end_j])
            out.append(chk + "\nBEGIN\n" + body + "\nEND")
            ins_cnt += 1
            if end_j < len(lines) and lines[end_j].strip() == 'GO':
                out.append('GO')
                end_j += 1
            i = end_j
            continue

        out.append(lines[i])
        i += 1
        continue

    # === ALTER TABLE ADD CONSTRAINT ===
    if 'ALTER TABLE' in ls and 'ADD' in ls and 'CONSTRAINT' in ls:
        m = re.search(r'ADD\s+CONSTRAINT\s+\[([^\]]+)\]', lines[i])
        if m:
            cn = m.group(1)
            blk = [lines[i]]
            j = i + 1
            while j < len(lines) and lines[j].strip() != 'GO':
                blk.append(lines[j])
                j += 1
            if j < len(lines):
                blk.append(lines[j])
                j += 1
            if (j < len(lines) and 'CHECK CONSTRAINT' in lines[j] 
                and cn in lines[j]):
                blk.append(lines[j])
                j += 1
                if j < len(lines) and lines[j].strip() == 'GO':
                    blk.append(lines[j])
                    j += 1
            body = '\n'.join([l for l in blk if l.strip() != 'GO'])
            wrapped = ("IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = N'" + cn +
                       "' AND schema_id = SCHEMA_ID(N'dbo'))\nBEGIN\n" + body + "\nEND\nGO")
            out.append(wrapped)
            con_cnt += 1
            i = j
            continue

    out.append(lines[i])
    i += 1

result = '\n'.join(out)

# --- Inject missing schema changes into the SQL ---

# 1. Add ImageUrl column to QuizQuestions CREATE TABLE (if not present)
if 'ImageUrl] [nvarchar](500) NULL' not in result:
    result = result.replace(
        '[AudioUrl] [nvarchar](max) NULL,\n\t[CorrectAnswerIndex]',
        '[AudioUrl] [nvarchar](max) NULL,\n\t[ImageUrl] [nvarchar](500) NULL,\n\t[CorrectAnswerIndex]'
    )
    print("  Injected: ImageUrl column into QuizQuestions")

# 2. Add ON DELETE CASCADE to FK_Enrollments_Courses_CourseId (if not present)
if 'FK_Enrollments_Courses_CourseId' in result:
    if not re.search(r'FK_Enrollments_Courses_CourseId.*?ON DELETE CASCADE', result, re.DOTALL):
        # The FK ALTER TABLE line is followed by REFERENCES line (both with \r\n line endings
        # from the original file). Insert ON DELETE CASCADE between REFERENCES and next line.
        old = ('ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT '
               '[FK_Enrollments_Courses_CourseId] FOREIGN KEY([CourseId])\r\n'
               'REFERENCES [dbo].[Courses] ([Id])\r\n')
        new = ('ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT '
               '[FK_Enrollments_Courses_CourseId] FOREIGN KEY([CourseId])\r\n'
               'REFERENCES [dbo].[Courses] ([Id])\r\n'
               'ON DELETE CASCADE\r\n')
        if old in result:
            result = result.replace(old, new, 1)
            print("  Injected: ON DELETE CASCADE into FK_Enrollments_Courses_CourseId")
        else:
            # Try without \r (in case lines were normalized)
            old2 = ('ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT '
                    '[FK_Enrollments_Courses_CourseId] Foreign KEY([CourseId])\n'
                    'References [dbo].[Courses] ([Id])\n')
            new2 = ('ALTER Table [dbo].[Enrollments]  WITH CHECK ADD  Constraint '
                    '[FK_Enrollments_Courses_CourseId] Foreign Key([CourseId])\n'
                    'References [dbo].[Courses] ([Id])\n'
                    'ON DELETE CASCADE\n')
            if old2 in result:
                result = result.replace(old2, new2, 1)
                print("  Injected: ON DELETE CASCADE into FK_Enrollments_Courses_CourseId (LF variant)")
            else:
                print("  WARNING: FK_Enrollments pattern NOT found for cascade injection")

# Write back as UTF-16LE with BOM
result_crlf = result.replace('\n', '\r\n')
with open('database.sql', 'wb') as f:
    f.write(b'\xff\xfe')  # UTF-16LE BOM
    f.write(result_crlf.encode('utf-16-le'))

print(f"Written: {len(result)} chars")
print(f"  Seed INSERTs wrapped: {ins_cnt}")
print(f"  IDENTITY blocks processed: {id_block_cnt}")
print(f"  Constraint wrappers: {con_cnt}")

for label, pat in [
    ('AspNetRoles', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles]'),
    ('AspNetUsers', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUsers]'),
    ('Courses', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[Courses]'),
    ('Enrollments', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[Enrollments]'),
    ('ChordSongs', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[ChordSongs]'),
    ('Lessons', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[Lessons]'),
    ('QuizQuestions', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[QuizQuestions]'),
    ('Quizzes', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[Quizzes]'),
    ('StudentAwards', 'IF NOT EXISTS (SELECT 1 FROM [dbo].[StudentAwards]'),
    ('FK_RC', "IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = N'FK_AspNetRoleClaims'"),
    ('FK_EC', "IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = N'FK_Enrollments_Courses_CourseId'"),
    ('DF_CS', "IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = N'DF_ChordSongs_AddedAt'"),
]:
    c = result.count(pat)
    status = "OK" if c > 0 else "MISSING"
    print(f"  {status}: {label} ({c})")

print("\nDone. Verify with: dotnet build")
