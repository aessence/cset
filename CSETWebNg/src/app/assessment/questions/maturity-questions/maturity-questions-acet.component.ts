////////////////////////////////
//
//   Copyright 2020 Battelle Energy Alliance, LLC
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
//
////////////////////////////////
import { Component, OnInit, AfterViewInit } from '@angular/core';
import { NavigationService } from '../../../services/navigation.service';
import { AssessmentService } from '../../../services/assessment.service';
import { MaturityService } from '../../../services/maturity.service';
import { QuestionsService } from '../../../services/questions.service';
import { QuestionGrouping, MaturityQuestionResponse } from '../../../models/questions.model';
import { MatDialogRef, MatDialog } from '@angular/material/dialog';
import { QuestionFiltersComponent } from '../../../dialogs/question-filters/question-filters.component';
import { QuestionFilterService } from '../../../services/question-filter.service';
import { ConfigService } from '../../../services/config.service';
import { GroupingBlockComponent } from '../grouping-block/grouping-block.component';
import { MaturityModel } from '../../../models/assessment-info.model';
import { QuestionsAcetService } from '../../../services/questions-acet.service';

@Component({
  selector: 'app-maturity-questions-acet',
  templateUrl: './maturity-questions-acet.component.html'
})
export class MaturityQuestionsAcetComponent implements OnInit, AfterViewInit {

  maturityLevels: any[];
  groupings: QuestionGrouping[] = null;
  modelName: string = null;
  showTargetLevel = false;    // TODO: set this from a new column in the DB

  loaded = false;

  filterDialogRef: MatDialogRef<QuestionFiltersComponent>;

  constructor(
    public assessSvc: AssessmentService,
    public configSvc: ConfigService,
    public maturitySvc: MaturityService,
    public questionsSvc: QuestionsService,
    public questionsAcetSvc: QuestionsAcetService,
    public filterSvc: QuestionFilterService,
    public navSvc: NavigationService,
    private dialog: MatDialog
  ) {

    if (this.assessSvc.assessment == null) {
      this.assessSvc.getAssessmentDetail().subscribe(
        (data: any) => {
          this.assessSvc.assessment = data;
        });
    }
  }

  ngOnInit() {
    this.loadQuestions();
    this.assessSvc.currentTab = 'questions';
  }

  /**
   *
   */
  ngAfterViewInit() {
  }


  /**
   * Returns the URL of the Questions page in the user guide.
   */
  helpDocUrl() {
    return this.configSvc.docUrl + 'htmlhelp/question_details__resources__and_comments.htm';
  }

  /**
   * Retrieves the complete list of questions
   */
  loadQuestions() {
    const magic = this.navSvc.getMagic();
    this.groupings = null;
    this.maturitySvc.getQuestionsList().subscribe(
      (response: MaturityQuestionResponse) => {        
        this.modelName = response.ModelName;
        this.maturityLevels = response.MaturityLevels;
        this.groupings = response.Groupings;
        this.assessSvc.assessment.MaturityModel.MaturityTargetLevel = response.MaturityTargetLevel;
        this.assessSvc.assessment.MaturityModel.AnswerOptions = response.AnswerOptions;
        this.loaded = true;

        // default the selected maturity filters
        this.questionsAcetSvc.initializeMatFilters(response.OverallIRP);

        this.refreshQuestionVisibility();
      },
      error => {
        console.log(
          'Error getting questions: ' +
          (<Error>error).name +
          (<Error>error).message
        );
        console.log('Error getting questions: ' + (<Error>error).stack);
      }
    );
  }

  /**
     * Controls the mass expansion/collapse of all subcategories on the screen.
     * @param mode
     */
  expandAll(mode: boolean) {
    this.groupings.forEach((g: QuestionGrouping) => {
      this.recurseExpansion(g, mode);
    });
  }

  /**
   * Groupings may be several levels deep so we need to recurse.
   */
  recurseExpansion(g: QuestionGrouping, mode: boolean) {
    g.Expanded = mode;
    g.SubGroupings.forEach((sg: QuestionGrouping) => {
      this.recurseExpansion(sg, mode);
    });
  }

  /**
   * 
   */
  showFilterDialog() {
    this.filterDialogRef = this.dialog.open(QuestionFiltersComponent, {
      data: {
        isMaturity: true
      }
    });

    this.filterDialogRef.componentInstance.filterChanged.asObservable().subscribe(() => {
      this.refreshQuestionVisibility();
    });

    this.filterDialogRef
      .afterClosed()
      .subscribe(() => {
        this.refreshQuestionVisibility();
      });
  }

  /**
 * Re-evaluates the visibility of all questions/subcategories/categories
 * based on the current filter settings.
 */
  refreshQuestionVisibility() {
    // this.questionsSvc.evaluateFilters(this.groupings);
  }
}