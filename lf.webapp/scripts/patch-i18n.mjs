import { readFileSync, writeFileSync } from 'node:fs';

function patch(path, locale) {
  const data = JSON.parse(readFileSync(path, 'utf8'));
  const isRu = locale === 'ru';
  const brand = isRu ? 'Школа Сильных Менеджеров' : 'High Managers School';
  const author = isRu ? 'Сергей Сидоров' : 'Sergey Sidorov';

  data.common.brand_name = brand;
  data.common.brand_short = isRu ? 'Сильные Менеджеры' : 'High Managers';
  data.common.author_name = author;

  const retitle = (key, mid) => {
    if (data[key]) data[key] = `${mid} — ${brand}`;
  };

  data.home_view_title = isRu
    ? `${brand} — практика для руководителей`
    : `${brand} — practice for managers`;
  retitle('login_view_title', isRu ? 'Вход для учеников' : 'Student login');
  retitle('courses_view_title', isRu ? 'Курсы' : 'Courses');
  retitle('events_view_title', isRu ? 'Мероприятия' : 'Events');
  retitle('certificates_view_title', isRu ? 'Сертификаты' : 'Certificates');
  retitle('profile_view_title', isRu ? 'Профиль' : 'Profile');
  retitle('admin_users_view_title', isRu ? 'Администрирование — Пользователи' : 'Administration — Users');
  retitle('admin_courses_view_title', isRu ? 'Администрирование — Курсы' : 'Administration — Courses');
  retitle('admin_categories_view_title', isRu ? 'Администрирование — Категории' : 'Administration — Categories');
  retitle('courses_available_view_title', isRu ? 'Доступные курсы' : 'Available courses');
  retitle('courses_active_view_title', isRu ? 'Активные курсы' : 'Active courses');
  retitle('courses_finished_view_title', isRu ? 'Завершённые курсы' : 'Finished courses');
  retitle('courses_teaching_view_title', isRu ? 'Преподавание' : 'Teaching');
  retitle('courses_create_view_title', isRu ? 'Новый курс' : 'New course');
  retitle('courses_edit_view_title', isRu ? 'Редактирование курса' : 'Edit course');
  retitle('courses_lesson_edit_view_title', isRu ? 'Редактирование урока' : 'Edit lesson');
  retitle('courses_learn_view_title', isRu ? 'Обучение' : 'Learning');

  data.nav.login = isRu ? 'Вход для учеников' : 'Student login';
  data.nav.administration = isRu ? 'Админка' : 'Admin';

  data.courses.eyebrow = isRu ? 'Каталог' : 'Catalog';
  data.courses.create_action = isRu ? 'Новый курс' : 'New course';
  data.courses.continue_draft = isRu
    ? 'У вас есть незавершённый черновик: «{title}»'
    : 'You have an unfinished draft: "{title}"';
  data.courses.search_placeholder = isRu ? 'Поиск по названию' : 'Search by title';

  data.courses.create.eyebrow = isRu ? 'Новый курс' : 'New course';
  data.courses.create.title = isRu ? 'Создать курс' : 'Create a course';
  data.courses.create.subtitle = isRu
    ? 'Задайте название, короткое описание, категорию и обложку — затем соберите главы в редакторе.'
    : 'Set the title, short pitch, category and cover — then assemble chapters in the editor.';
  data.courses.create.submit = isRu ? 'Создать курс' : 'Create course';
  data.courses.create.submitting = isRu ? 'Создание…' : 'Creating…';
  data.courses.create.your_drafts_empty = isRu
    ? 'Черновиков пока нет.'
    : 'No drafts yet.';

  data.profile.eyebrow = isRu ? 'Аккаунт' : 'Account';
  data.profile.subtitle = isRu
    ? 'Имя и контакты для платформы. Держите данные актуальными — сертификаты привязаны к профилю.'
    : 'Name and contact details for the platform. Keep them current so certificates stay attributed correctly.';

  data.login.eyebrow = isRu ? 'Вход для учеников' : 'Student login';
  data.login.title = isRu ? 'Войти' : 'Sign in';
  data.login.subtitle = isRu
    ? 'Выберите аккаунт. Дальше вход продолжит выбранный провайдер.'
    : 'Choose the account you use for learning. You will continue through the selected identity provider.';
  data.login.pmi.description = isRu
    ? 'Используйте аккаунт PMI Club, чтобы войти.'
    : 'Use your PMI Club account to sign in.';
  data.login.google.description = isRu
    ? 'Используйте аккаунт Google, чтобы войти.'
    : 'Use your Google account to sign in.';

  data.events.hello = isRu ? 'Раздел появится позже.' : 'This section is coming later.';
  data.certificates.hello = isRu ? 'Раздел появится позже.' : 'This section is coming later.';

  data.home = {
    hero: {
      brand_line: brand,
      headline: isRu ? 'Стать сильнее как руководитель' : 'Become a stronger manager',
      subheadline: isRu
        ? 'Практика от практика: агенты на LLM и Kanban для тех, кто уже ведёт работу — не конвейер курсов, один преподаватель.'
        : 'Practice from a practitioner: LLM agents and Kanban for people who already ship work — one instructor, no assembly line.',
      cta_primary: isRu ? 'Смотреть курсы' : 'See the courses',
      trust_line: isRu
        ? 'Практика в основе · реальные инструменты · один автор'
        : 'Practice-first · real tools · one instructor',
    },
    facts: {
      items: isRu
        ? [
            { value: 'Практика', label: 'у автора, не слайды ради слайдов' },
            { value: '2', label: 'стартовых курса' },
            { value: '1', label: 'преподаватель, без конвейера' },
          ]
        : [
            { value: 'Practice', label: 'from the person who ships, not slide decks' },
            { value: '2', label: 'starting courses' },
            { value: '1', label: 'instructor, no assembly line' },
          ],
    },
    audience: data.home.audience,
    courses: {
      title: isRu ? 'Курсы' : 'Courses',
      subtitle: isRu
        ? 'Первые программы школы — прикладные, короткие по входу, с упором на работу, а не на сертификат.'
        : 'The first programs — practical, low on ceremony, built around real work rather than certificates.',
      cta: isRu ? 'Подробнее' : 'Learn more',
      filters: {
        all: isRu ? 'Все' : 'All',
        ai: 'AI',
        flow: isRu ? 'Поток' : 'Flow',
        management: isRu ? 'Управление' : 'Management',
      },
      items: {
        llm_agentic: {
          ...data.home.courses.items.llm_agentic,
          filter: 'ai',
        },
        kanban: {
          ...data.home.courses.items.kanban,
          filter: 'flow',
        },
      },
    },
    author: {
      ...data.home.author,
      title: isRu ? 'Преподаватель' : 'Instructor',
      subtitle: isRu
        ? 'Не теоретик со слайдов — человек, который делает.'
        : 'Not a slide-deck theorist — someone who ships.',
      url: 'https://s-sidorov.ru',
    },
    faq: {
      title: isRu ? 'Вопросы' : 'Questions',
      items: {
        1: {
          question: isRu ? 'Кому подходит школа?' : 'Who is this school for?',
          answer: isRu
            ? 'Руководителям проектов и продуктов, системным аналитикам и смежным ролям в IT, которым нужна практика, а не сертификат ради сертификата.'
            : 'Project and product managers, systems analysts, and neighbouring IT roles who want practical skills — not certificates for their own sake.',
        },
        2: {
          question: isRu ? 'Нужно ли уже уметь программировать?' : 'Do I need to already know how to code?',
          answer: isRu
            ? 'Зависит от курса. Ни один из стартовых курсов не требует обязательного опыта в программировании — уточняйте описание перед записью.'
            : 'It depends on the course. Neither starting course has a hard coding prerequisite — check the description before enrolling.',
        },
        3: {
          question: isRu ? 'Как проходит обучение?' : 'How are courses delivered?',
          answer: isRu
            ? 'В своём темпе на платформе: главы и уроки с текстом, изображениями, видео и аудио, с отслеживанием прогресса.'
            : 'Self-paced on the platform: chapters and lessons with text, images, video, and audio, with progress tracked as you go.',
        },
        4: {
          question: isRu ? 'Могу я предложить тему курса?' : 'Can I suggest a course topic?',
          answer: isRu
            ? "Да. Напишите на hello{'@'}s-sidorov.ru — мы сохраним запрос, и новые курсы во многом появляются именно так."
            : "Yes. Email hello{'@'}s-sidorov.ru — we keep every request, and courses can be shaped by what people actually ask for.",
        },
        5: {
          question: isRu ? 'Кто ведёт школу?' : 'Who runs this school?',
          answer: isRu
            ? 'Пока один человек — Сергей Сидоров. Подробнее в разделе преподавателя и на s-sidorov.ru.'
            : 'One person so far — Sergey Sidorov. See the instructor section and s-sidorov.ru.',
        },
      },
    },
    contacts: {
      title: isRu ? 'Контакты' : 'Contacts',
      subtitle: isRu
        ? 'Если возникли вопросы — напишите, отвечаем в течение суток.'
        : 'Questions? Write us — we reply within a day.',
      support: 'hello{\'@\'}s-sidorov.ru',
      notify_title: isRu ? 'Узнать о новых курсах' : 'Get notified about new courses',
      notify_subtitle: isRu
        ? 'Напишем, когда выйдет новый курс — без спама.'
        : "We'll email you when a new course launches — no spam.",
      notify_subject: isRu ? 'Сообщите о новых курсах' : 'Notify me about new courses',
      notify_cta: isRu ? 'Написать' : 'Email us',
    },
  };

  delete data.vuestic;
  writeFileSync(path, `${JSON.stringify(data, null, 4)}\n`);
}

patch('src/i18n/locales/en.json', 'en');
patch('src/i18n/locales/ru.json', 'ru');
console.log('patched locales');
