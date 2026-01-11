import React, { useState, useEffect } from 'react';
import './ReadingPlan.css';

const ReadingPlan = () => {
  const [currentDay, setCurrentDay] = useState(1);
  const [completedDays, setCompletedDays] = useState([]);
  const [progress, setProgress] = useState(0);

  // Sample reading plan data (Day 1-365)
  const readingPlanData = {
    1: { old: ['Genesis 1-3'], new: ['Matthew 1'] },
    2: { old: ['Genesis 4-7'], new: ['Matthew 2'] },
    3: { old: ['Genesis 8-11'], new: ['Matthew 3'] },
    // Add more days as needed
  };

  useEffect(() => {
    // Load progress from localStorage
    const saved = localStorage.getItem('readingProgress');
    if (saved) {
      const data = JSON.parse(saved);
      setCompletedDays(data.completed || []);
      setCurrentDay(data.currentDay || 1);
    }
  }, []);

  useEffect(() => {
    setProgress((completedDays.length / 365) * 100);
    // Save progress to localStorage
    localStorage.setItem('readingProgress', JSON.stringify({
      completed: completedDays,
      currentDay
    }));
  }, [completedDays, currentDay]);

  const markDayComplete = (day) => {
    if (!completedDays.includes(day)) {
      setCompletedDays([...completedDays, day]);
    }
  };

  const getTodaysReading = () => {
    return readingPlanData[currentDay] || { old: [], new: [] };
  };

  return (
    <div className="reading-plan-container">
      <h1>Bible Reading Plan - Read the Bible in a Year</h1>
      
      <div className="progress-section">
        <h2>Your Progress</h2>
        <div className="progress-bar">
          <div 
            className="progress-fill" 
            style={{ width: `${progress}%` }}
          >
            {progress.toFixed(1)}%
          </div>
        </div>
        <p>{completedDays.length} of 365 days completed</p>
      </div>

      <div className="daily-reading">
        <h2>Day {currentDay} Reading</h2>
        <div className="reading-sections">
          <div className="old-testament">
            <h3>Old Testament</h3>
            <ul>
              {getTodaysReading().old.map((passage, idx) => (
                <li key={idx}>{passage}</li>
              ))}
            </ul>
          </div>
          <div className="new-testament">
            <h3>New Testament</h3>
            <ul>
              {getTodaysReading().new.map((passage, idx) => (
                <li key={idx}>{passage}</li>
              ))}
            </ul>
          </div>
        </div>
        <button 
          onClick={() => markDayComplete(currentDay)}
          disabled={completedDays.includes(currentDay)}
          className="complete-btn"
        >
          {completedDays.includes(currentDay) ? '✓ Completed' : 'Mark as Complete'}
        </button>
        <div className="navigation-buttons">
          <button 
            onClick={() => setCurrentDay(Math.max(1, currentDay - 1))}
            disabled={currentDay === 1}
          >
            Previous Day
          </button>
          <button 
            onClick={() => setCurrentDay(Math.min(365, currentDay + 1))}
            disabled={currentDay === 365}
          >
            Next Day
          </button>
        </div>
      </div>
    </div>
  );
};

export default ReadingPlan;