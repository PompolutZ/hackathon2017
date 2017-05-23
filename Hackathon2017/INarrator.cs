using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Speech.Synthesis;

namespace Hackathon2017
{
    public interface INarrator
    {
        void Say(string phrase);

        void Cancel(string phrase);
    }

    public class SimpleNarrator : INarrator
    {
        private readonly SpeechSynthesizer _speechSynthesizer;
        private ConcurrentDictionary<string, Prompt> _textPhraseMap = new ConcurrentDictionary<string, Prompt>();
        public SimpleNarrator()
        {
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.Rate = 5;
        }

        public void Say(string phrase)
        {
            var prompt = _speechSynthesizer.SpeakAsync(phrase);
            _textPhraseMap.AddOrUpdate(phrase, _ => prompt, (_, __) => prompt);
        }

        public void Cancel(string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
                return;

            Prompt p;
            if (_textPhraseMap.TryGetValue(phrase, out p))
            {
                _speechSynthesizer.SpeakAsyncCancel(p);
                _textPhraseMap.TryRemove(phrase, out p);
            }
            //_speechSynthesizer.SpeakAsyncCancelAll();
        }
    }
}